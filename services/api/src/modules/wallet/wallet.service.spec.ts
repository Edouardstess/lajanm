import { BadRequestException, NotFoundException } from '@nestjs/common';
import { randomUUID } from 'crypto';
import { DataSource } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { User, UserTier } from '../auth/entities/user.entity';
import { FraudService } from '../fraud/fraud.service';
import { Account, AccountOwnerType } from '../ledger/entities/account.entity';
import { LedgerService } from '../ledger/ledger.service';
import { AccountsService } from '../ledger/services/accounts.service';
import { NotificationsService } from '../notifications/notifications.service';
import { SecurityService } from '../security/security.service';
import { FakeLedgerDataSource } from '../../testing/fake-ledger-data-source';
import { WalletService } from './wallet.service';

const sender: User = { id: 'user-1', phone: '+50900000001', tier: UserTier.BASIC } as User;
const recipient: User = { id: 'user-2', phone: '+50900000002', tier: UserTier.BASIC } as User;

const senderAccount: Account = {
  id: 'acc-1',
  ownerType: AccountOwnerType.USER,
  ownerId: 'user-1',
  name: 'wallet',
  currency: 'HTG',
  createdAt: new Date(),
};
const recipientAccount: Account = {
  id: 'acc-2',
  ownerType: AccountOwnerType.USER,
  ownerId: 'user-2',
  name: 'wallet',
  currency: 'HTG',
  createdAt: new Date(),
};

function buildService(options: { securityService?: Partial<SecurityService> } = {}) {
  const fakeLedgerDataSource = new FakeLedgerDataSource();
  const ledgerService = new LedgerService(fakeLedgerDataSource as unknown as DataSource);

  const users = {
    findOneByOrFail: jest.fn().mockResolvedValue(sender),
    findOneBy: jest.fn().mockImplementation(async (where: Partial<User>) => {
      if (where.phone === recipient.phone) return recipient;
      if (where.phone === sender.phone) return sender;
      return null;
    }),
  };
  const accountsService = {
    getOrCreateUserWalletAccount: jest.fn().mockImplementation(async (userId: string) =>
      userId === sender.id ? senderAccount : recipientAccount,
    ),
  } as unknown as AccountsService;
  const auditService = { record: jest.fn().mockResolvedValue(undefined) } as unknown as AuditService;
  const notificationsService = { notify: jest.fn().mockResolvedValue(undefined) } as unknown as NotificationsService;
  const securityService = {
    enforceLimits: jest.fn().mockResolvedValue(undefined),
    enforceOtpIfRequired: jest.fn().mockResolvedValue(undefined),
    ...options.securityService,
  } as unknown as SecurityService;
  const fraudService = { evaluate: jest.fn().mockResolvedValue(undefined) } as unknown as FraudService;

  const service = new WalletService(
    users as unknown as never,
    ledgerService,
    accountsService,
    auditService,
    notificationsService,
    securityService,
    fraudService,
  );

  return { service, ledgerService, securityService, fraudService, notificationsService };
}

const fundingSourceAccountId = 'system-funding-source';

async function fund(ledgerService: LedgerService, accountId: string, amountMinor: bigint) {
  await ledgerService.postOperation({
    idempotencyKey: `fund-${randomUUID()}`,
    type: 'adjustment' as never,
    entries: [
      { accountId, direction: 'credit' as never, amountMinor },
      { accountId: fundingSourceAccountId, direction: 'debit' as never, amountMinor },
    ],
  });
}

describe('WalletService.transfer', () => {
  it('rejects a transfer to an unknown phone number', async () => {
    const { service } = buildService();
    await expect(
      service.transfer(sender.id, {
        recipientPhone: '+50999999999',
        amountHTG: 100,
        clientRequestId: randomUUID(),
      }),
    ).rejects.toThrow(NotFoundException);
  });

  it('rejects a transfer to your own account', async () => {
    const { service } = buildService();
    await expect(
      service.transfer(sender.id, {
        recipientPhone: sender.phone,
        amountHTG: 100,
        clientRequestId: randomUUID(),
      }),
    ).rejects.toThrow('Cannot transfer to your own account');
  });

  it('rejects a transfer that would overdraw the sender', async () => {
    const { service } = buildService();
    await expect(
      service.transfer(sender.id, {
        recipientPhone: recipient.phone,
        amountHTG: 100,
        clientRequestId: randomUUID(),
      }),
    ).rejects.toThrow('Insufficient balance');
  });

  it('moves funds and evaluates fraud rules only for a real (non-idempotent) transfer', async () => {
    const { service, ledgerService, fraudService, notificationsService } = buildService();
    await fund(ledgerService, senderAccount.id, 50_000n);

    const clientRequestId = randomUUID();
    const first = await service.transfer(sender.id, {
      recipientPhone: recipient.phone,
      amountHTG: 200,
      clientRequestId,
    });
    expect(first.idempotent).toBe(false);
    expect(await ledgerService.getBalance(senderAccount.id)).toBe(30_000n);
    expect(await ledgerService.getBalance(recipientAccount.id)).toBe(20_000n);
    expect(fraudService.evaluate).toHaveBeenCalledTimes(1);
    expect(notificationsService.notify).toHaveBeenCalledTimes(2);

    // Retried with the same clientRequestId: idempotent, no second
    // evaluation/notification, no further balance movement.
    const second = await service.transfer(sender.id, {
      recipientPhone: recipient.phone,
      amountHTG: 200,
      clientRequestId,
    });
    expect(second.idempotent).toBe(true);
    expect(await ledgerService.getBalance(senderAccount.id)).toBe(30_000n);
    expect(fraudService.evaluate).toHaveBeenCalledTimes(1);
    expect(notificationsService.notify).toHaveBeenCalledTimes(2);
  });

  it('rejects when SecurityService says the transfer would exceed the tier limit, before moving money', async () => {
    const { service, ledgerService } = buildService({
      securityService: {
        enforceLimits: jest.fn().mockRejectedValue(new BadRequestException('exceeds daily limit')),
      },
    });
    await fund(ledgerService, senderAccount.id, 50_000n);

    await expect(
      service.transfer(sender.id, {
        recipientPhone: recipient.phone,
        amountHTG: 200,
        clientRequestId: randomUUID(),
      }),
    ).rejects.toThrow('exceeds daily limit');
    expect(await ledgerService.getBalance(senderAccount.id)).toBe(50_000n);
  });

  it('rejects when SecurityService says an OTP is required but none was provided', async () => {
    const { service, ledgerService, securityService } = buildService({
      securityService: {
        enforceOtpIfRequired: jest.fn().mockRejectedValue(new BadRequestException('An OTP is required')),
      },
    });
    await fund(ledgerService, senderAccount.id, 50_000n);

    await expect(
      service.transfer(sender.id, {
        recipientPhone: recipient.phone,
        amountHTG: 200,
        clientRequestId: randomUUID(),
      }),
    ).rejects.toThrow('An OTP is required');
    expect(securityService.enforceOtpIfRequired).toHaveBeenCalledWith('user-1', 'transfer', 200, undefined);
  });
});
