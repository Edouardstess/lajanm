import { randomUUID } from 'crypto';
import { DataSource } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { Account, AccountOwnerType } from '../ledger/entities/account.entity';
import { LedgerService } from '../ledger/ledger.service';
import { AccountsService } from '../ledger/services/accounts.service';
import { FakeLedgerDataSource } from '../../testing/fake-ledger-data-source';
import { ComplianceService } from './compliance.service';
import { Dispute, DisputeStatus } from './entities/dispute.entity';

const floatAccount: Account = {
  id: 'float-acc',
  ownerType: AccountOwnerType.SYSTEM,
  ownerId: null,
  name: 'moncash_float',
  currency: 'HTG',
  createdAt: new Date(),
};

function createDisputesRepo() {
  const rows: Dispute[] = [];
  return {
    rows,
    create: jest.fn(
      (data: Partial<Dispute>) =>
        ({
          id: randomUUID(),
          status: DisputeStatus.OPEN,
          internalNotes: null,
          assignedTo: null,
          relatedOperationId: null,
          createdAt: new Date(),
          updatedAt: new Date(),
          ...data,
        }) as Dispute,
    ),
    save: jest.fn(async (entity: Dispute) => {
      const i = rows.findIndex((r) => r.id === entity.id);
      if (i === -1) rows.push(entity);
      else rows[i] = entity;
      return entity;
    }),
    findOneBy: jest.fn(async (where: Partial<Dispute>) => rows.find((r) => r.id === where.id) ?? null),
    find: jest.fn(async () => [...rows]),
  };
}

function buildService(walletTotal: string, floatBalanceMinor: bigint) {
  const disputes = createDisputesRepo();
  const sars = { create: jest.fn(), save: jest.fn(), find: jest.fn() };
  const dataSource = { query: jest.fn().mockResolvedValue([{ total: walletTotal }]) };
  const fakeLedgerDataSource = new FakeLedgerDataSource();
  const ledgerService = new LedgerService(fakeLedgerDataSource as unknown as DataSource);
  const accountsService = {
    getOrCreateSystemAccount: jest.fn().mockResolvedValue(floatAccount),
  } as unknown as AccountsService;
  const auditService = { record: jest.fn().mockResolvedValue(undefined) } as unknown as AuditService;

  // Seed the float account's balance in the fake ledger directly.
  if (floatBalanceMinor !== 0n) {
    fakeLedgerDataSource.entries.push({
      id: randomUUID(),
      accountId: floatAccount.id,
      operationId: 'seed-op',
      direction: floatBalanceMinor > 0n ? 'credit' : 'debit',
      amountMinor: (floatBalanceMinor > 0n ? floatBalanceMinor : -floatBalanceMinor).toString(),
      currency: 'HTG',
      createdAt: new Date(),
    });
  }

  const service = new ComplianceService(
    disputes as unknown as never,
    sars as unknown as never,
    dataSource as unknown as DataSource,
    ledgerService,
    accountsService,
    auditService,
  );

  return { service, disputes };
}

describe('ComplianceService.getReconciliation', () => {
  it('reports balanced when the wallet total exactly offsets the float balance', async () => {
    const { service } = buildService('50000', -50000n);
    const report = await service.getReconciliation();

    expect(report.internalWalletTotalMinor).toBe('50000');
    expect(report.moncashFloatBalanceMinor).toBe('-50000');
    expect(report.discrepancyMinor).toBe('0');
    expect(report.isBalanced).toBe(true);
  });

  it('flags an imbalance when the two figures do not net to zero', async () => {
    const { service } = buildService('50000', -40000n);
    const report = await service.getReconciliation();

    expect(report.discrepancyMinor).toBe('10000');
    expect(report.isBalanced).toBe(false);
  });
});

describe('ComplianceService disputes', () => {
  it('creates a dispute and lets an admin update its status and notes', async () => {
    const { service, disputes } = buildService('0', 0n);
    const dispute = await service.createDispute('user-1', {
      subject: 'Missing funds',
      description: 'Transfer never arrived',
    });
    expect(disputes.rows).toHaveLength(1);

    const updated = await service.updateDispute(dispute.id, 'admin-1', {
      status: DisputeStatus.RESOLVED,
      internalNotes: 'Refunded manually',
    });
    expect(updated.status).toBe(DisputeStatus.RESOLVED);
    expect(updated.internalNotes).toBe('Refunded manually');
  });
});
