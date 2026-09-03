import { randomUUID } from 'crypto';
import { QueryFailedError } from 'typeorm';
import { Operation } from '../modules/ledger/entities/operation.entity';

/**
 * In-memory stand-in for TypeORM's DataSource, faithful to the two
 * behaviours LedgerService actually relies on:
 *  - transaction() rolls back only what it itself inserted if the
 *    callback throws (mirroring per-transaction Postgres isolation — a
 *    concurrent transaction's already-committed writes must never be
 *    wiped by another transaction's rollback)
 *  - a duplicate idempotencyKey throws a QueryFailedError carrying code
 *    '23505', exactly like a real unique constraint violation, and
 *    updating an existing row by id never trips that check
 *
 * Shared by ledger.service.spec.ts, topup.service.spec.ts, and
 * payout.service.spec.ts so each module's tests exercise a real
 * LedgerService instance (proving actual invariants) instead of a mocked
 * one, without re-deriving this fake three times.
 */
export class FakeLedgerDataSource {
  operations: Array<Record<string, unknown>> = [];
  entries: Array<Record<string, unknown>> = [];

  async transaction<T>(cb: (manager: unknown) => Promise<T>): Promise<T> {
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
            create: (data: Record<string, unknown>) => ({ id: randomUUID(), ...data }),
            save: async (data: Record<string, unknown>) => {
              const existingIndex = this.operations.findIndex((o) => o.id === data.id);
              if (existingIndex !== -1) {
                this.operations[existingIndex] = data;
                return data;
              }
              if (this.operations.some((o) => o.idempotencyKey === data.idempotencyKey)) {
                const driverError = Object.assign(new Error('duplicate key'), { code: '23505' });
                throw new QueryFailedError('INSERT INTO operations ...', [], driverError);
              }
              this.operations.push(data);
              insertedOperations.push(data);
              return data;
            },
            update: async (criteria: Record<string, unknown>, partial: Record<string, unknown>) => {
              const found = this.operations.find((o) =>
                Object.entries(criteria).every(([k, v]) => o[k] === v),
              );
              if (found) Object.assign(found, partial);
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
          create: (data: Record<string, unknown>) => ({ id: randomUUID(), ...data }),
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
