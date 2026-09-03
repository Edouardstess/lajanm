import 'reflect-metadata';
import * as dotenv from 'dotenv';
import { DataSource } from 'typeorm';
import { Account, AccountOwnerType } from './entities/account.entity';
import { EntryDirection } from './entities/ledger-entry.entity';
import { OperationType } from './entities/operation.entity';
import { LedgerService } from './ledger.service';

dotenv.config();

// Requires a real, migrated Postgres — see jest.integration.config.js.
const runIfConfigured = process.env.DATABASE_URL ? describe : describe.skip;

runIfConfigured('LedgerService against a real Postgres instance', () => {
  let dataSource: DataSource;
  let ledgerService: LedgerService;
  let accountA: Account;
  let accountB: Account;

  beforeAll(async () => {
    dataSource = new DataSource({
      type: 'postgres',
      url: process.env.DATABASE_URL,
      entities: [__dirname + '/../../**/*.entity{.ts,.js}'],
    });
    await dataSource.initialize();
    ledgerService = new LedgerService(dataSource);

    const accountRepo = dataSource.getRepository(Account);
    accountA = await accountRepo.save(
      accountRepo.create({ ownerType: AccountOwnerType.SYSTEM, name: `it-a-${Date.now()}` }),
    );
    accountB = await accountRepo.save(
      accountRepo.create({ ownerType: AccountOwnerType.SYSTEM, name: `it-b-${Date.now()}` }),
    );
  });

  afterAll(async () => {
    await dataSource.destroy();
  });

  it('computes a real balance via SUM over ledger_entries (raw SQL, real column casing)', async () => {
    await ledgerService.postOperation({
      idempotencyKey: `it-${Date.now()}-1`,
      type: OperationType.ADJUSTMENT,
      entries: [
        { accountId: accountA.id, direction: EntryDirection.CREDIT, amountMinor: 12_345n },
        { accountId: accountB.id, direction: EntryDirection.DEBIT, amountMinor: 12_345n },
      ],
    });

    // This is the exact call that failed with "column entry.amountminor
    // does not exist" against a real Postgres before the fix — the
    // in-memory unit test double can't catch that class of bug because it
    // never executes real SQL.
    expect(await ledgerService.getBalance(accountA.id)).toBe(12_345n);
    expect(await ledgerService.getBalance(accountB.id)).toBe(-12_345n);
  });

  it('lists entries with the operation type joined in (real SQL, not the fake DataSource)', async () => {
    const entries = await ledgerService.listEntries(accountA.id, { limit: 10 });
    expect(entries.length).toBeGreaterThan(0);
    expect(entries[0].operationType).toBe(OperationType.ADJUSTMENT);
  });
});
