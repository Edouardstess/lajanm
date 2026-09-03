import { ConfigService } from '@nestjs/config';
import { randomUUID } from 'crypto';
import { DataSource } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { EntryDirection } from '../ledger/entities/ledger-entry.entity';
import { LedgerService } from '../ledger/ledger.service';
import { FakeLedgerDataSource } from '../../testing/fake-ledger-data-source';
import { FraudFlag, FraudFlagStatus, FraudRuleCode } from './entities/fraud-flag.entity';
import { FraudService } from './fraud.service';

function createFlagsRepo() {
  const rows: FraudFlag[] = [];
  return {
    rows,
    create: jest.fn(
      (data: Partial<FraudFlag>) =>
        ({
          id: randomUUID(),
          status: FraudFlagStatus.OPEN,
          relatedOperationId: null,
          details: null,
          resolvedBy: null,
          resolvedAt: null,
          createdAt: new Date(),
          ...data,
        }) as FraudFlag,
    ),
    save: jest.fn(async (entity: FraudFlag) => {
      const i = rows.findIndex((r) => r.id === entity.id);
      if (i === -1) rows.push(entity);
      else rows[i] = entity;
      return entity;
    }),
    findOneBy: jest.fn(async (where: Partial<FraudFlag>) => rows.find((r) => r.id === where.id) ?? null),
    find: jest.fn(async () => rows.filter((r) => r.status === FraudFlagStatus.OPEN)),
  };
}

function buildService(options: { configValues?: Record<string, unknown>; newBeneficiaryCount?: number } = {}) {
  const flags = createFlagsRepo();
  const fakeLedgerDataSource = new FakeLedgerDataSource();
  const ledgerService = new LedgerService(fakeLedgerDataSource as unknown as DataSource);
  const config = new ConfigService(options.configValues ?? {});
  const auditService = { record: jest.fn().mockResolvedValue(undefined) } as unknown as AuditService;
  const dataSource = {
    query: jest.fn().mockResolvedValue([{ count: String(options.newBeneficiaryCount ?? 0) }]),
  };

  const service = new FraudService(
    flags as unknown as never,
    dataSource as unknown as DataSource,
    config,
    ledgerService,
    auditService,
  );

  return { service, flags, ledgerService, dataSource };
}

describe('FraudService.evaluate', () => {
  it('flags high velocity once the debit count within the window hits the threshold', async () => {
    const { service, flags, ledgerService } = buildService({
      configValues: { FRAUD_VELOCITY_COUNT_THRESHOLD: 3, FRAUD_VELOCITY_WINDOW_MINUTES: 10 },
    });

    let lastOperationId = '';
    for (let i = 0; i < 3; i += 1) {
      const result = await ledgerService.postOperation({
        idempotencyKey: `velocity-${i}`,
        type: 'transfer' as never,
        entries: [
          { accountId: 'acc-1', direction: EntryDirection.DEBIT, amountMinor: 100n },
          { accountId: 'acc-2', direction: EntryDirection.CREDIT, amountMinor: 100n },
        ],
      });
      lastOperationId = result.operation.id;
    }

    await service.evaluate({
      userId: 'user-1',
      accountId: 'acc-1',
      operationId: lastOperationId,
      amountMinor: 100n,
    });

    expect(flags.rows).toHaveLength(1);
    expect(flags.rows[0].ruleCode).toBe(FraudRuleCode.HIGH_VELOCITY);
  });

  it('does not flag velocity when the count is under the threshold', async () => {
    const { service, flags, ledgerService } = buildService({
      configValues: { FRAUD_VELOCITY_COUNT_THRESHOLD: 10 },
    });
    const result = await ledgerService.postOperation({
      idempotencyKey: 'velocity-solo',
      type: 'transfer' as never,
      entries: [
        { accountId: 'acc-1', direction: EntryDirection.DEBIT, amountMinor: 100n },
        { accountId: 'acc-2', direction: EntryDirection.CREDIT, amountMinor: 100n },
      ],
    });

    await service.evaluate({
      userId: 'user-1',
      accountId: 'acc-1',
      operationId: result.operation.id,
      amountMinor: 100n,
    });

    expect(flags.rows).toHaveLength(0);
  });

  it('flags an amount far above the account’s recent average', async () => {
    const { service, flags, ledgerService } = buildService({
      configValues: { FRAUD_VELOCITY_COUNT_THRESHOLD: 1000, FRAUD_AMOUNT_MULTIPLIER: 5 },
    });

    // Five prior small debits establish a ~100n average.
    for (let i = 0; i < 5; i += 1) {
      await ledgerService.postOperation({
        idempotencyKey: `history-${i}`,
        type: 'transfer' as never,
        entries: [
          { accountId: 'acc-1', direction: EntryDirection.DEBIT, amountMinor: 100n },
          { accountId: 'acc-2', direction: EntryDirection.CREDIT, amountMinor: 100n },
        ],
      });
    }

    const spike = await ledgerService.postOperation({
      idempotencyKey: 'spike',
      type: 'transfer' as never,
      entries: [
        { accountId: 'acc-1', direction: EntryDirection.DEBIT, amountMinor: 10_000n },
        { accountId: 'acc-2', direction: EntryDirection.CREDIT, amountMinor: 10_000n },
      ],
    });

    await service.evaluate({
      userId: 'user-1',
      accountId: 'acc-1',
      operationId: spike.operation.id,
      amountMinor: 10_000n,
    });

    expect(flags.rows.map((f) => f.ruleCode)).toContain(FraudRuleCode.AMOUNT_ANOMALY);
  });

  it('does not flag amount anomaly without enough transaction history', async () => {
    const { service, flags, ledgerService } = buildService({
      configValues: { FRAUD_VELOCITY_COUNT_THRESHOLD: 1000 },
    });
    const result = await ledgerService.postOperation({
      idempotencyKey: 'no-history',
      type: 'transfer' as never,
      entries: [
        { accountId: 'acc-1', direction: EntryDirection.DEBIT, amountMinor: 50_000n },
        { accountId: 'acc-2', direction: EntryDirection.CREDIT, amountMinor: 50_000n },
      ],
    });

    await service.evaluate({
      userId: 'user-1',
      accountId: 'acc-1',
      operationId: result.operation.id,
      amountMinor: 50_000n,
    });

    expect(flags.rows).toHaveLength(0);
  });

  it('flags frequent new beneficiaries using the distinct-recipient count from the database', async () => {
    const { service, flags, ledgerService, dataSource } = buildService({
      configValues: { FRAUD_VELOCITY_COUNT_THRESHOLD: 1000, FRAUD_NEW_BENEFICIARY_THRESHOLD: 3 },
      newBeneficiaryCount: 4,
    });
    const result = await ledgerService.postOperation({
      idempotencyKey: 'new-beneficiary',
      type: 'transfer' as never,
      entries: [
        { accountId: 'acc-1', direction: EntryDirection.DEBIT, amountMinor: 100n },
        { accountId: 'acc-2', direction: EntryDirection.CREDIT, amountMinor: 100n },
      ],
    });

    await service.evaluate({
      userId: 'user-1',
      accountId: 'acc-1',
      operationId: result.operation.id,
      amountMinor: 100n,
      recipientId: 'recipient-1',
    });

    expect(dataSource.query).toHaveBeenCalled();
    expect(flags.rows.map((f) => f.ruleCode)).toContain(FraudRuleCode.FREQUENT_NEW_BENEFICIARIES);
  });

  it('never throws — a fraud-detection bug must not fail the underlying transaction', async () => {
    const { service, ledgerService } = buildService();
    (ledgerService.countDirection as unknown) = jest.fn().mockRejectedValue(new Error('boom'));

    await expect(
      service.evaluate({ userId: 'user-1', accountId: 'acc-1', operationId: 'op-1', amountMinor: 100n }),
    ).resolves.toBeUndefined();
  });
});
