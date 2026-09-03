import 'reflect-metadata';
import * as dotenv from 'dotenv';
import { DataSource } from 'typeorm';
import { Account, AccountOwnerType } from './entities/account.entity';
import { EntryDirection, LedgerEntry } from './entities/ledger-entry.entity';
import { Operation, OperationStatus, OperationType } from './entities/operation.entity';
import { LedgerService } from './ledger.service';

dotenv.config();

/**
 * The invariants that actually protect customer money, exercised against a
 * real Postgres. Each one here is either enforced *by the database* (a
 * unique constraint, an immutability trigger) or depends on real
 * transactional behaviour — none of it can be proven by the in-memory test
 * double, which is precisely why these live in the integration tier.
 *
 * Requires a migrated database — see jest.integration.config.js.
 */
const runIfConfigured = process.env.DATABASE_URL ? describe : describe.skip;

runIfConfigured('Ledger invariants (real Postgres)', () => {
  let dataSource: DataSource;
  let ledger: LedgerService;
  let accountA: Account;
  let accountB: Account;

  const uniqueKey = (label: string) => `it-${label}-${Date.now()}-${Math.random().toString(16).slice(2)}`;

  beforeAll(async () => {
    dataSource = new DataSource({
      type: 'postgres',
      url: process.env.DATABASE_URL,
      entities: [__dirname + '/../../**/*.entity{.ts,.js}'],
    });
    await dataSource.initialize();
    ledger = new LedgerService(dataSource);

    const accounts = dataSource.getRepository(Account);
    const suffix = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    accountA = await accounts.save(
      accounts.create({ ownerType: AccountOwnerType.SYSTEM, name: `inv-a-${suffix}` }),
    );
    accountB = await accounts.save(
      accounts.create({ ownerType: AccountOwnerType.SYSTEM, name: `inv-b-${suffix}` }),
    );
  });

  afterAll(async () => {
    await dataSource.destroy();
  });

  describe('idempotency', () => {
    it('posting the same idempotency key twice moves money exactly once', async () => {
      const key = uniqueKey('idem');
      const post = () =>
        ledger.postOperation({
          idempotencyKey: key,
          type: OperationType.ADJUSTMENT,
          entries: [
            { accountId: accountA.id, direction: EntryDirection.CREDIT, amountMinor: 5_000n },
            { accountId: accountB.id, direction: EntryDirection.DEBIT, amountMinor: 5_000n },
          ],
        });

      const before = await ledger.getBalance(accountA.id);
      const first = await post();
      const second = await post();

      expect(first.idempotent).toBe(false);
      // The retry must report success referring to the ORIGINAL operation —
      // a caller such as the MonCash webhook handler treats this as a
      // normal success, so returning a different id would silently split
      // one payment into two records.
      expect(second.idempotent).toBe(true);
      expect(second.operation.id).toBe(first.operation.id);

      expect(await ledger.getBalance(accountA.id)).toBe(before + 5_000n);

      const operations = await dataSource
        .getRepository(Operation)
        .findBy({ idempotencyKey: key });
      expect(operations).toHaveLength(1);

      const entries = await dataSource
        .getRepository(LedgerEntry)
        .findBy({ operationId: first.operation.id });
      expect(entries).toHaveLength(2);
    });

    it('survives concurrent posts of the same key without double-spending', async () => {
      const key = uniqueKey('race');
      const before = await ledger.getBalance(accountA.id);

      // The real hazard: Postgres aborts the whole transaction on a unique
      // violation, so the loser of this race must re-read the winning
      // operation on a *fresh* connection. Getting that wrong (as an
      // earlier version of postOperation did) surfaces only under genuine
      // concurrency, never in a sequential test.
      const results = await Promise.all(
        Array.from({ length: 5 }, () =>
          ledger.postOperation({
            idempotencyKey: key,
            type: OperationType.ADJUSTMENT,
            entries: [
              { accountId: accountA.id, direction: EntryDirection.CREDIT, amountMinor: 700n },
              { accountId: accountB.id, direction: EntryDirection.DEBIT, amountMinor: 700n },
            ],
          }),
        ),
      );

      const ids = new Set(results.map((r) => r.operation.id));
      expect(ids.size).toBe(1);
      expect(results.filter((r) => !r.idempotent)).toHaveLength(1);
      expect(await ledger.getBalance(accountA.id)).toBe(before + 700n);
    });
  });

  describe('balance integrity', () => {
    it('refuses an unbalanced operation, leaving no partial write behind', async () => {
      const before = await ledger.getBalance(accountA.id);

      await expect(
        ledger.postOperation({
          idempotencyKey: uniqueKey('unbalanced'),
          type: OperationType.ADJUSTMENT,
          entries: [
            { accountId: accountA.id, direction: EntryDirection.CREDIT, amountMinor: 900n },
            { accountId: accountB.id, direction: EntryDirection.DEBIT, amountMinor: 800n },
          ],
        }),
      ).rejects.toThrow();

      expect(await ledger.getBalance(accountA.id)).toBe(before);
    });

    it('reverses an operation with mirrored entries rather than mutating it', async () => {
      const before = await ledger.getBalance(accountA.id);

      const original = await ledger.postOperation({
        idempotencyKey: uniqueKey('rev-src'),
        type: OperationType.ADJUSTMENT,
        entries: [
          { accountId: accountA.id, direction: EntryDirection.CREDIT, amountMinor: 2_500n },
          { accountId: accountB.id, direction: EntryDirection.DEBIT, amountMinor: 2_500n },
        ],
      });
      expect(await ledger.getBalance(accountA.id)).toBe(before + 2_500n);

      await ledger.reverseOperation(original.operation.id, uniqueKey('rev'));

      // Net zero again, and the original entries are still on the record.
      expect(await ledger.getBalance(accountA.id)).toBe(before);

      const originalEntries = await dataSource
        .getRepository(LedgerEntry)
        .findBy({ operationId: original.operation.id });
      expect(originalEntries).toHaveLength(2);

      const reloaded = await dataSource
        .getRepository(Operation)
        .findOneByOrFail({ id: original.operation.id });
      expect(reloaded.status).toBe(OperationStatus.REVERSED);
    });

    it('confirms a pending operation without touching its entries', async () => {
      const posted = await ledger.postOperation({
        idempotencyKey: uniqueKey('confirm'),
        type: OperationType.PAYOUT,
        status: OperationStatus.PENDING,
        entries: [
          { accountId: accountA.id, direction: EntryDirection.DEBIT, amountMinor: 400n },
          { accountId: accountB.id, direction: EntryDirection.CREDIT, amountMinor: 400n },
        ],
      });

      const balanceWhilePending = await ledger.getBalance(accountA.id);
      await ledger.confirmOperation(posted.operation.id);

      const reloaded = await dataSource
        .getRepository(Operation)
        .findOneByOrFail({ id: posted.operation.id });
      expect(reloaded.status).toBe(OperationStatus.COMPLETED);
      // Confirming is a status change only: the money already moved when
      // the reservation was posted.
      expect(await ledger.getBalance(accountA.id)).toBe(balanceWhilePending);
    });
  });

  describe('append-only enforcement at the database layer', () => {
    // The application never issues these statements; the trigger is the
    // backstop for anything that bypasses it — a future bug, a migration,
    // or a human with a psql prompt.
    it('rejects an UPDATE against ledger_entries', async () => {
      const posted = await ledger.postOperation({
        idempotencyKey: uniqueKey('immutable-u'),
        type: OperationType.ADJUSTMENT,
        entries: [
          { accountId: accountA.id, direction: EntryDirection.CREDIT, amountMinor: 100n },
          { accountId: accountB.id, direction: EntryDirection.DEBIT, amountMinor: 100n },
        ],
      });

      await expect(
        dataSource.query('UPDATE ledger_entries SET "amountMinor" = 999999 WHERE id = $1', [
          posted.entries[0].id,
        ]),
      ).rejects.toThrow();
    });

    it('rejects a DELETE against ledger_entries', async () => {
      const posted = await ledger.postOperation({
        idempotencyKey: uniqueKey('immutable-d'),
        type: OperationType.ADJUSTMENT,
        entries: [
          { accountId: accountA.id, direction: EntryDirection.CREDIT, amountMinor: 100n },
          { accountId: accountB.id, direction: EntryDirection.DEBIT, amountMinor: 100n },
        ],
      });

      await expect(
        dataSource.query('DELETE FROM ledger_entries WHERE id = $1', [posted.entries[0].id]),
      ).rejects.toThrow();

      const stillThere = await dataSource
        .getRepository(LedgerEntry)
        .findOneBy({ id: posted.entries[0].id });
      expect(stillThere).not.toBeNull();
    });

    it('rejects an UPDATE and a DELETE against audit_logs', async () => {
      const inserted = await dataSource.query(
        `INSERT INTO audit_logs ("action", "actorId", "actorType")
         VALUES ('test.immutability', $1, 'system') RETURNING id`,
        [accountA.id],
      );
      const id = inserted[0].id;

      await expect(
        dataSource.query("UPDATE audit_logs SET action = 'tampered' WHERE id = $1", [id]),
      ).rejects.toThrow();
      await expect(dataSource.query('DELETE FROM audit_logs WHERE id = $1', [id])).rejects.toThrow();
    });
  });
});
