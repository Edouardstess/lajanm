import { BadRequestException, Injectable } from '@nestjs/common';
import { InjectDataSource } from '@nestjs/typeorm';
import { DataSource, EntityManager, QueryFailedError } from 'typeorm';
import { LedgerEntry, EntryDirection } from './entities/ledger-entry.entity';
import { Operation, OperationStatus, OperationType } from './entities/operation.entity';

const POSTGRES_UNIQUE_VIOLATION = '23505';

export interface LedgerEntryInput {
  accountId: string;
  direction: EntryDirection;
  amountMinor: bigint;
  currency?: string;
}

export interface PostOperationInput {
  idempotencyKey: string;
  type: OperationType;
  entries: LedgerEntryInput[];
  metadata?: Record<string, unknown>;
  status?: OperationStatus;
}

export interface PostOperationResult {
  operation: Operation;
  entries: LedgerEntry[];
  // true when the idempotencyKey already existed and no new entries were
  // written — callers (e.g. a MonCash webhook handler) must treat this the
  // same as a fresh success, not as an error.
  idempotent: boolean;
}

/**
 * Owns the double-entry ledger. This is the only place in the codebase
 * allowed to write to ledger_entries — every other module (topup, payout,
 * wallet, ...) must go through postOperation() rather than touching the
 * table directly, so the balance-per-currency and idempotency invariants
 * can never be bypassed.
 */
@Injectable()
export class LedgerService {
  constructor(@InjectDataSource() private readonly dataSource: DataSource) {}

  async postOperation(input: PostOperationInput): Promise<PostOperationResult> {
    try {
      return await this.dataSource.transaction((manager) =>
        this.postOperationWithManager(manager, input),
      );
    } catch (error) {
      // A unique violation aborts the whole Postgres transaction, so the
      // fallback lookup below MUST run outside it, on a fresh connection —
      // querying with the same (now-aborted) transaction would itself throw
      // "current transaction is aborted, commands ignored until end of
      // transaction block".
      if (this.isUniqueViolation(error)) {
        return this.loadOperationByIdempotencyKey(input.idempotencyKey, true);
      }
      throw error;
    }
  }

  private async loadOperationByIdempotencyKey(
    idempotencyKey: string,
    idempotent: boolean,
  ): Promise<PostOperationResult> {
    const operation = await this.dataSource
      .getRepository(Operation)
      .findOneByOrFail({ idempotencyKey });
    const entries = await this.dataSource
      .getRepository(LedgerEntry)
      .findBy({ operationId: operation.id });
    return { operation, entries, idempotent };
  }

  /**
   * Reverses a completed operation by posting a new, balanced Operation
   * whose entries mirror the original with debit/credit swapped. The
   * original operation and its entries are never mutated or deleted —
   * this is how the ledger stays append-only even when a payout fails
   * after funds were reserved.
   */
  async reverseOperation(
    originalOperationId: string,
    reversalIdempotencyKey: string,
  ): Promise<PostOperationResult> {
    return this.dataSource.transaction(async (manager) => {
      const operationRepo = manager.getRepository(Operation);
      const entryRepo = manager.getRepository(LedgerEntry);

      const original = await operationRepo.findOneByOrFail({ id: originalOperationId });
      const originalEntries = await entryRepo.findBy({ operationId: originalOperationId });

      if (originalEntries.length === 0) {
        throw new BadRequestException('Cannot reverse an operation with no entries');
      }

      const mirrored = originalEntries.map((entry) => ({
        accountId: entry.accountId,
        direction:
          entry.direction === EntryDirection.DEBIT ? EntryDirection.CREDIT : EntryDirection.DEBIT,
        amountMinor: BigInt(entry.amountMinor),
        currency: entry.currency,
      }));

      const result = await this.postOperationWithManager(manager, {
        idempotencyKey: reversalIdempotencyKey,
        type: OperationType.ADJUSTMENT,
        entries: mirrored,
        metadata: { reversalOf: originalOperationId },
      });

      original.status = OperationStatus.REVERSED;
      await operationRepo.save(original);

      return result;
    });
  }

  /** Balance = sum(credits) - sum(debits), always derived, never stored. */
  async getBalance(accountId: string, currency = 'HTG'): Promise<bigint> {
    const row = await this.dataSource
      .getRepository(LedgerEntry)
      .createQueryBuilder('entry')
      .select(
        `COALESCE(SUM(CASE WHEN entry.direction = :credit THEN entry.amountMinor ELSE -entry.amountMinor END), 0)`,
        'balance',
      )
      .where('entry.accountId = :accountId', { accountId })
      .andWhere('entry.currency = :currency', { currency })
      .setParameter('credit', EntryDirection.CREDIT)
      .getRawOne<{ balance: string }>();

    return BigInt(row?.balance ?? '0');
  }

  private async postOperationWithManager(
    manager: EntityManager,
    input: PostOperationInput,
  ): Promise<PostOperationResult> {
    this.assertBalanced(input.entries);
    const operationRepo = manager.getRepository(Operation);
    const entryRepo = manager.getRepository(LedgerEntry);

    const operation = await operationRepo.save(
      operationRepo.create({
        idempotencyKey: input.idempotencyKey,
        type: input.type,
        status: input.status ?? OperationStatus.COMPLETED,
        metadata: input.metadata ?? null,
      }),
    );

    const entries = await entryRepo.save(
      input.entries.map((entry) =>
        entryRepo.create({
          operationId: operation.id,
          accountId: entry.accountId,
          direction: entry.direction,
          amountMinor: entry.amountMinor.toString(),
          currency: entry.currency ?? 'HTG',
        }),
      ),
    );

    return { operation, entries, idempotent: false };
  }

  private assertBalanced(entries: LedgerEntryInput[]): void {
    if (entries.length < 2) {
      throw new BadRequestException('An operation requires at least two ledger entries');
    }

    const totalsByCurrency = new Map<string, bigint>();
    for (const entry of entries) {
      if (entry.amountMinor <= 0n) {
        throw new BadRequestException('Ledger entry amounts must be strictly positive');
      }
      const currency = entry.currency ?? 'HTG';
      const signed = entry.direction === EntryDirection.CREDIT ? entry.amountMinor : -entry.amountMinor;
      totalsByCurrency.set(currency, (totalsByCurrency.get(currency) ?? 0n) + signed);
    }

    for (const [currency, total] of totalsByCurrency) {
      if (total !== 0n) {
        throw new BadRequestException(
          `Unbalanced operation for currency ${currency}: debits and credits must sum to zero`,
        );
      }
    }
  }

  private isUniqueViolation(error: unknown): boolean {
    return (
      error instanceof QueryFailedError &&
      (error as unknown as { code?: string }).code === POSTGRES_UNIQUE_VIOLATION
    );
  }
}
