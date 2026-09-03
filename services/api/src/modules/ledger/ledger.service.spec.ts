import { randomUUID } from 'crypto';
import { BadRequestException } from '@nestjs/common';
import { DataSource, QueryFailedError } from 'typeorm';
import { EntryDirection } from './entities/ledger-entry.entity';
import { Operation, OperationType } from './entities/operation.entity';
import { LedgerService } from './ledger.service';

/**
 * An in-memory stand-in for TypeORM's DataSource, faithful to the two
 * behaviours this service actually relies on:
 *  - transaction() rolls back everything it wrote if the callback throws
 *    (mirroring Postgres aborting the transaction on a constraint violation)
 *  - a duplicate idempotencyKey throws a QueryFailedError carrying
 *    code '23505', exactly like a real unique constraint violation
 * This lets the idempotency and balance invariants be tested without a
 * real Postgres instance; docker-compose is used for full integration
 * testing against the actual migrations (see docs/architecture.md).
 */
class FakeDataSource {
  private operations: Array<Record<string, unknown>> = [];
  private entries: Array<Record<string, unknown>> = [];

  async transaction<T>(cb: (manager: unknown) => Promise<T>): Promise<T> {
    // Real Postgres transactions are isolated per-connection: rolling back
    // transaction B never undoes rows transaction A already committed.
    // Modeling rollback as "restore a snapshot taken when this transaction
    // started" would violate that — a concurrent transaction that committed
    // in between would have its writes silently wiped. So instead each
    // transaction tracks only the rows it itself inserted, and undoes
    // exactly those on failure.
    const insertedOperations: Array<Record<string, unknown>> = [];
    const insertedEntries: Array<Record<string, unknown>> = [];
    try {
      return await cb(this.manager(insertedOperations, insertedEntries));
    } catch (error) {
      this.operations = this.operations.filter((o) => !insertedOperations.includes(o));
      this.entries = this.entries.filter((e) => !insertedEntries.includes(e));
      throw error;
    }
  }

  getRepository(entityClass: unknown) {
    return this.manager([], []).getRepository(entityClass);
  }

  private manager(
    insertedOperations: Array<Record<string, unknown>>,
    insertedEntries: Array<Record<string, unknown>>,
  ) {
    return {
      getRepository: (entityClass: unknown) => {
        if (entityClass === Operation) {
          return {
            create: (data: Record<string, unknown>) => ({
              id: randomUUID(),
              createdAt: new Date(),
              updatedAt: new Date(),
              ...data,
            }),
            save: async (data: Record<string, unknown>) => {
              const existingIndex = this.operations.findIndex((o) => o.id === data.id);
              if (existingIndex !== -1) {
                // Same primary key: this is an update (e.g. marking an
                // operation REVERSED), not a fresh insert, so it must not
                // trip the idempotencyKey duplicate check below.
                this.operations[existingIndex] = data;
                return data;
              }
              if (this.operations.some((o) => o.idempotencyKey === data.idempotencyKey)) {
                const driverError = Object.assign(
                  new Error('duplicate key value violates unique constraint'),
                  { code: '23505' },
                );
                throw new QueryFailedError('INSERT INTO operations ...', [], driverError);
              }
              this.operations.push(data);
              insertedOperations.push(data);
              return data;
            },
            findOneByOrFail: async (where: Record<string, unknown>) => {
              const found = this.operations.find((o) =>
                Object.entries(where).every(([k, v]) => o[k] === v),
              );
              if (!found) throw new Error('Operation not found');
              return found;
            },
          };
        }
        // LedgerEntry repository
        return {
          create: (data: Record<string, unknown>) => ({
            id: randomUUID(),
            createdAt: new Date(),
            ...data,
          }),
          save: async (data: Array<Record<string, unknown>>) => {
            this.entries.push(...data);
            insertedEntries.push(...data);
            return data;
          },
          findBy: async (where: Record<string, unknown>) =>
            this.entries.filter((e) => Object.entries(where).every(([k, v]) => e[k] === v)),
          createQueryBuilder: () => {
            const params: Record<string, unknown> = {};
            const builder = {
              select: () => builder,
              where: (_expr: string, p: Record<string, unknown>) => {
                Object.assign(params, p);
                return builder;
              },
              andWhere: (_expr: string, p: Record<string, unknown>) => {
                Object.assign(params, p);
                return builder;
              },
              setParameter: (key: string, value: unknown) => {
                params[key] = value;
                return builder;
              },
              getRawOne: async () => {
                const relevant = this.entries.filter(
                  (e) => e.accountId === params.accountId && e.currency === params.currency,
                );
                let balance = 0n;
                for (const e of relevant) {
                  const amount = BigInt(e.amountMinor as string);
                  balance += e.direction === params.credit ? amount : -amount;
                }
                return { balance: balance.toString() };
              },
            };
            return builder;
          },
        };
      },
    };
  }
}

describe('LedgerService', () => {
  let dataSource: FakeDataSource;
  let service: LedgerService;
  const accountA = randomUUID();
  const accountB = randomUUID();

  beforeEach(() => {
    dataSource = new FakeDataSource();
    service = new LedgerService(dataSource as unknown as DataSource);
  });

  it('posts a balanced operation and computes balances from the entries', async () => {
    await service.postOperation({
      idempotencyKey: 'topup-1',
      type: OperationType.TOPUP,
      entries: [
        { accountId: accountA, direction: EntryDirection.CREDIT, amountMinor: 10_000n },
        { accountId: accountB, direction: EntryDirection.DEBIT, amountMinor: 10_000n },
      ],
    });

    expect(await service.getBalance(accountA)).toBe(10_000n);
    expect(await service.getBalance(accountB)).toBe(-10_000n);
  });

  it('rejects an operation whose debits and credits do not sum to zero', async () => {
    await expect(
      service.postOperation({
        idempotencyKey: 'bad-op',
        type: OperationType.TRANSFER,
        entries: [
          { accountId: accountA, direction: EntryDirection.CREDIT, amountMinor: 500n },
          { accountId: accountB, direction: EntryDirection.DEBIT, amountMinor: 400n },
        ],
      }),
    ).rejects.toThrow(BadRequestException);
  });

  it('never creates two movements for the same idempotency key, even retried concurrently', async () => {
    const post = () =>
      service.postOperation({
        idempotencyKey: 'moncash-ref-123',
        type: OperationType.TOPUP,
        entries: [
          { accountId: accountA, direction: EntryDirection.CREDIT, amountMinor: 25_000n },
          { accountId: accountB, direction: EntryDirection.DEBIT, amountMinor: 25_000n },
        ],
      });

    // Simulates MonCash resending the same webhook: two "concurrent" calls
    // with the identical idempotency key must never double-credit the user.
    const [first, second] = await Promise.all([post(), post()]);

    expect([first.idempotent, second.idempotent].sort()).toEqual([false, true]);
    expect(first.operation.id).toBe(second.operation.id);
    expect(await service.getBalance(accountA)).toBe(25_000n);
  });

  it('reverses an operation with mirrored entries and marks it reversed, without mutating the originals', async () => {
    const { operation, entries: originalEntries } = await service.postOperation({
      idempotencyKey: 'payout-1',
      type: OperationType.PAYOUT,
      entries: [
        { accountId: accountA, direction: EntryDirection.DEBIT, amountMinor: 5_000n },
        { accountId: accountB, direction: EntryDirection.CREDIT, amountMinor: 5_000n },
      ],
    });

    await service.reverseOperation(operation.id, 'payout-1-reversal');

    expect(await service.getBalance(accountA)).toBe(0n);
    expect(await service.getBalance(accountB)).toBe(0n);
    // The original entries themselves are untouched — reversal is additive.
    expect(originalEntries).toHaveLength(2);
  });

  it('rejects entries with a non-positive amount', async () => {
    await expect(
      service.postOperation({
        idempotencyKey: 'zero-amount',
        type: OperationType.TRANSFER,
        entries: [
          { accountId: accountA, direction: EntryDirection.CREDIT, amountMinor: 0n },
          { accountId: accountB, direction: EntryDirection.DEBIT, amountMinor: 0n },
        ],
      }),
    ).rejects.toThrow(BadRequestException);
  });
});
