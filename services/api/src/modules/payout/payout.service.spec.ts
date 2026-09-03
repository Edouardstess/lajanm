import { BadRequestException } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { randomUUID } from 'crypto';
import { DataSource } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { User, UserTier } from '../auth/entities/user.entity';
import { FraudService } from '../fraud/fraud.service';
import { Account, AccountOwnerType } from '../ledger/entities/account.entity';
import { EntryDirection } from '../ledger/entities/ledger-entry.entity';
import { LedgerService } from '../ledger/ledger.service';
import { AccountsService } from '../ledger/services/accounts.service';
import { NotificationsService } from '../notifications/notifications.service';
import { SecurityService } from '../security/security.service';
import { MonCashClient, MonCashUnavailableError } from '../topup/moncash-client.service';
import { FakeLedgerDataSource } from '../../testing/fake-ledger-data-source';
import { PayoutStatus, PayoutTransaction } from './entities/payout-transaction.entity';
import { PayoutService } from './payout.service';

function createTransactionsRepo() {
  const rows: PayoutTransaction[] = [];
  return {
    rows,
    create: jest.fn(
      (data: Partial<PayoutTransaction>) =>
        ({
          id: randomUUID(),
          reversalOperationId: null,
          providerReference: null,
          failureReason: null,
          currency: 'HTG',
          createdAt: new Date(),
          updatedAt: new Date(),
          ...data,
        }) as PayoutTransaction,
    ),
    save: jest.fn(async (entity: PayoutTransaction) => {
      const i = rows.findIndex((r) => r.id === entity.id);
      if (i === -1) rows.push(entity);
      else rows[i] = entity;
      return entity;
    }),
    findOneBy: jest.fn(async (where: Partial<PayoutTransaction>) => {
      const keys = Object.keys(where) as Array<keyof PayoutTransaction>;
      return rows.find((r) => keys.every((k) => r[k] === where[k])) ?? null;
    }),
  };
}

const testUser: User = {
  id: 'user-1',
  phone: '+50900000001',
  tier: UserTier.BASIC,
} as User;

const walletAccount: Account = {
  id: 'wallet-acc',
  ownerType: AccountOwnerType.USER,
  ownerId: 'user-1',
  name: 'wallet',
  currency: 'HTG',
  createdAt: new Date(),
};
const floatAccount: Account = {
  id: 'float-acc',
  ownerType: AccountOwnerType.SYSTEM,
  ownerId: null,
  name: 'moncash_float',
  currency: 'HTG',
  createdAt: new Date(),
};

function buildService(options: {
  fakeLedgerDataSource: FakeLedgerDataSource;
  monCashClient: MonCashClient;
  configValues?: Record<string, unknown>;
  securityService?: Partial<SecurityService>;
}) {
  const transactions = createTransactionsRepo();
  const users = { findOneByOrFail: jest.fn().mockResolvedValue(testUser) };
  const ledgerService = new LedgerService(options.fakeLedgerDataSource as unknown as DataSource);
  const accountsService = {
    getOrCreateUserWalletAccount: jest.fn().mockResolvedValue(walletAccount),
    getOrCreateSystemAccount: jest.fn().mockResolvedValue(floatAccount),
  } as unknown as AccountsService;
  const auditService = { record: jest.fn().mockResolvedValue(undefined) } as unknown as AuditService;
  const notificationsService = { notify: jest.fn().mockResolvedValue(undefined) } as unknown as NotificationsService;
  const securityService = {
    enforceLimits: jest.fn().mockResolvedValue(undefined),
    enforceOtpIfRequired: jest.fn().mockResolvedValue(undefined),
    ...options.securityService,
  } as unknown as SecurityService;
  const fraudService = { evaluate: jest.fn().mockResolvedValue(undefined) } as unknown as FraudService;
  const config = new ConfigService(options.configValues ?? {});

  const service = new PayoutService(
    transactions as unknown as never,
    users as unknown as never,
    config,
    ledgerService,
    accountsService,
    auditService,
    notificationsService,
    options.monCashClient,
    securityService,
    fraudService,
  );

  return { service, transactions, ledgerService, securityService, fraudService };
}

async function fundWallet(ledgerService: LedgerService, amountMinor: bigint) {
  await ledgerService.postOperation({
    idempotencyKey: `fund-${randomUUID()}`,
    type: 'adjustment' as never,
    entries: [
      { accountId: walletAccount.id, direction: EntryDirection.CREDIT, amountMinor },
      { accountId: floatAccount.id, direction: EntryDirection.DEBIT, amountMinor },
    ],
  });
}

describe('PayoutService', () => {
  it('rejects a payout above the configured per-transaction cap', async () => {
    const fakeLedgerDataSource = new FakeLedgerDataSource();
    const monCashClient = { createPayout: jest.fn() } as unknown as MonCashClient;
    const { service } = buildService({
      fakeLedgerDataSource,
      monCashClient,
      configValues: { PAYOUT_MAX_AMOUNT_HTG: 100_000 },
    });

    await expect(
      service.initiate('user-1', { amountHTG: 150_000, clientRequestId: randomUUID() }),
    ).rejects.toThrow(BadRequestException);
    expect(monCashClient.createPayout).not.toHaveBeenCalled();
  });

  it('rejects a payout that would overdraw the wallet', async () => {
    const fakeLedgerDataSource = new FakeLedgerDataSource();
    const monCashClient = { createPayout: jest.fn() } as unknown as MonCashClient;
    const { service } = buildService({ fakeLedgerDataSource, monCashClient });

    await expect(
      service.initiate('user-1', { amountHTG: 500, clientRequestId: randomUUID() }),
    ).rejects.toThrow('Insufficient balance');
  });

  it('debits the wallet and confirms the operation on a successful MonCash payout', async () => {
    const fakeLedgerDataSource = new FakeLedgerDataSource();
    const monCashClient = {
      createPayout: jest.fn().mockResolvedValue({ reference: 'moncash-payout-ref' }),
    } as unknown as MonCashClient;
    const { service, transactions, ledgerService } = buildService({ fakeLedgerDataSource, monCashClient });

    await fundWallet(ledgerService, 100_000n);
    const result = await service.initiate('user-1', { amountHTG: 300, clientRequestId: randomUUID() });

    expect(result.status).toBe(PayoutStatus.COMPLETED);
    expect(await ledgerService.getBalance(walletAccount.id)).toBe(70_000n);
    expect(transactions.rows[0].status).toBe(PayoutStatus.COMPLETED);
    expect(transactions.rows[0].providerReference).toBe('moncash-payout-ref');
  });

  it('reverses the reserved debit and restores the balance when MonCash cannot complete the payout', async () => {
    const fakeLedgerDataSource = new FakeLedgerDataSource();
    const monCashClient = {
      createPayout: jest.fn().mockRejectedValue(new MonCashUnavailableError('down')),
    } as unknown as MonCashClient;
    const { service, transactions, ledgerService } = buildService({ fakeLedgerDataSource, monCashClient });

    await fundWallet(ledgerService, 100_000n);
    const result = await service.initiate('user-1', { amountHTG: 300, clientRequestId: randomUUID() });

    expect(result.status).toBe(PayoutStatus.FAILED);
    // Balance is back to exactly what it was before the attempt — the
    // reserve debit was mirrored by a reversing credit, not deleted.
    expect(await ledgerService.getBalance(walletAccount.id)).toBe(100_000n);
    expect(transactions.rows[0].status).toBe(PayoutStatus.FAILED);
    expect(transactions.rows[0].reversalOperationId).toBeTruthy();
  });

  it('never calls MonCash twice for the same clientRequestId', async () => {
    const fakeLedgerDataSource = new FakeLedgerDataSource();
    const monCashClient = {
      createPayout: jest.fn().mockResolvedValue({ reference: 'moncash-payout-ref-2' }),
    } as unknown as MonCashClient;
    const { service, ledgerService } = buildService({ fakeLedgerDataSource, monCashClient });

    await fundWallet(ledgerService, 100_000n);
    const clientRequestId = randomUUID();

    const first = await service.initiate('user-1', { amountHTG: 200, clientRequestId });
    const second = await service.initiate('user-1', { amountHTG: 200, clientRequestId });

    expect(second.payoutTransactionId).toBe(first.payoutTransactionId);
    expect(monCashClient.createPayout).toHaveBeenCalledTimes(1);
  });

  it('rejects the payout when SecurityService says the tier limit would be exceeded, before touching MonCash', async () => {
    const fakeLedgerDataSource = new FakeLedgerDataSource();
    const monCashClient = { createPayout: jest.fn() } as unknown as MonCashClient;
    const { service, ledgerService } = buildService({
      fakeLedgerDataSource,
      monCashClient,
      securityService: {
        enforceLimits: jest.fn().mockRejectedValue(new BadRequestException('exceeds daily limit')),
      },
    });

    await fundWallet(ledgerService, 100_000n);
    await expect(
      service.initiate('user-1', { amountHTG: 300, clientRequestId: randomUUID() }),
    ).rejects.toThrow('exceeds daily limit');
    expect(monCashClient.createPayout).not.toHaveBeenCalled();
    // Nothing was reserved either — the limit check runs before the debit.
    expect(await ledgerService.getBalance(walletAccount.id)).toBe(100_000n);
  });

  it('requires a verified OTP when SecurityService says one is needed, before touching MonCash', async () => {
    const fakeLedgerDataSource = new FakeLedgerDataSource();
    const monCashClient = { createPayout: jest.fn() } as unknown as MonCashClient;
    const { service, ledgerService, securityService } = buildService({
      fakeLedgerDataSource,
      monCashClient,
      securityService: {
        enforceOtpIfRequired: jest.fn().mockRejectedValue(new BadRequestException('An OTP is required')),
      },
    });

    await fundWallet(ledgerService, 100_000n);
    await expect(
      service.initiate('user-1', { amountHTG: 300, clientRequestId: randomUUID() }),
    ).rejects.toThrow('An OTP is required');
    expect(securityService.enforceOtpIfRequired).toHaveBeenCalledWith(
      'user-1',
      'payout',
      300,
      undefined,
    );
    expect(monCashClient.createPayout).not.toHaveBeenCalled();
  });
});
