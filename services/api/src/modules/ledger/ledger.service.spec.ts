import { randomUUID } from 'crypto';
import { BadRequestException } from '@nestjs/common';
import { DataSource } from 'typeorm';
import { FakeLedgerDataSource } from '../../testing/fake-ledger-data-source';
import { EntryDirection } from './entities/ledger-entry.entity';
import { Operation, OperationStatus, OperationType } from './entities/operation.entity';
import { LedgerService } from './ledger.service';

describe('LedgerService', () => {
  let dataSource: FakeLedgerDataSource;
  let service: LedgerService;
  const accountA = randomUUID();
  const accountB = randomUUID();

  beforeEach(() => {
    dataSource = new FakeLedgerDataSource();
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

  it('confirms a pending operation, and leaves a non-pending one untouched', async () => {
    const { operation } = await service.postOperation({
      idempotencyKey: 'payout-2',
      type: OperationType.PAYOUT,
      status: OperationStatus.PENDING,
      entries: [
        { accountId: accountA, direction: EntryDirection.DEBIT, amountMinor: 1_000n },
        { accountId: accountB, direction: EntryDirection.CREDIT, amountMinor: 1_000n },
      ],
    });

    const operationRepo = (dataSource as unknown as DataSource).getRepository(Operation);

    await service.confirmOperation(operation.id);
    let updated = await operationRepo.findOneByOrFail({ id: operation.id });
    expect(updated.status).toBe(OperationStatus.COMPLETED);

    // Confirming again (e.g. a duplicate provider callback) must not
    // clobber a status that already moved on — here it's a no-op because
    // the row is no longer PENDING.
    await service.reverseOperation(operation.id, 'payout-2-reversal');
    await service.confirmOperation(operation.id);
    updated = await operationRepo.findOneByOrFail({ id: operation.id });
    expect(updated.status).toBe(OperationStatus.REVERSED);
  });

  it('sums only entries in the requested direction since the given date', async () => {
    await service.postOperation({
      idempotencyKey: 'sum-1',
      type: OperationType.TOPUP,
      entries: [
        { accountId: accountA, direction: EntryDirection.CREDIT, amountMinor: 3_000n },
        { accountId: accountB, direction: EntryDirection.DEBIT, amountMinor: 3_000n },
      ],
    });
    await service.postOperation({
      idempotencyKey: 'sum-2',
      type: OperationType.TRANSFER,
      entries: [
        { accountId: accountA, direction: EntryDirection.DEBIT, amountMinor: 1_000n },
        { accountId: accountB, direction: EntryDirection.CREDIT, amountMinor: 1_000n },
      ],
    });

    const since = new Date(Date.now() - 60_000);
    expect(await service.sumDirection(accountA, EntryDirection.CREDIT, since)).toBe(3_000n);
    expect(await service.sumDirection(accountA, EntryDirection.DEBIT, since)).toBe(1_000n);

    const future = new Date(Date.now() + 60_000);
    expect(await service.sumDirection(accountA, EntryDirection.CREDIT, future)).toBe(0n);
  });

  it('counts entries in one direction since a given date', async () => {
    await service.postOperation({
      idempotencyKey: 'count-1',
      type: OperationType.TRANSFER,
      entries: [
        { accountId: accountA, direction: EntryDirection.DEBIT, amountMinor: 100n },
        { accountId: accountB, direction: EntryDirection.CREDIT, amountMinor: 100n },
      ],
    });
    await service.postOperation({
      idempotencyKey: 'count-2',
      type: OperationType.TRANSFER,
      entries: [
        { accountId: accountA, direction: EntryDirection.DEBIT, amountMinor: 50n },
        { accountId: accountB, direction: EntryDirection.CREDIT, amountMinor: 50n },
      ],
    });

    const since = new Date(Date.now() - 60_000);
    expect(await service.countDirection(accountA, EntryDirection.DEBIT, since)).toBe(2);
    expect(await service.countDirection(accountA, EntryDirection.CREDIT, since)).toBe(0);
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
