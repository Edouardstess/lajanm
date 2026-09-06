import { BadRequestException, NotFoundException } from '@nestjs/common';
import { randomUUID } from 'crypto';
import { DataSource } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { User, UserTier } from '../auth/entities/user.entity';
import { FraudService } from '../fraud/fraud.service';
import { Account, AccountOwnerType } from '../ledger/entities/account.entity';
import { LedgerService } from '../ledger/ledger.service';
import { AccountsService } from '../ledger/services/accounts.service';
import { RateLimitService } from '../../common/rate-limit.service';
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

function buildService(
  options: { securityService?: Partial<SecurityService>; rateLimitService?: Partial<RateLimitService> } = {},
) {
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
  const rateLimitService = {
    consume: jest.fn().mockResolvedValue(undefined),
    ...options.rateLimitService,
  } as unknown as RateLimitService;

  const service = new WalletService(
    users as unknown as never,
    ledgerService,
    accountsService,
    auditService,
    notificationsService,
    securityService,
    fraudService,
    rateLimitService,
  );

  return { service, ledgerService, securityService, fraudService, notificationsService, rateLimitService, users };
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
  // --- Numéros de téléphone -------------------------------------------
  //
  // Le défaut corrigé : le destinataire était cherché sur la chaîne
  // exacte saisie. « 39000001 » ne trouvait donc pas le compte enregistré
  // sous « +50939000001 », et l'expéditeur recevait « ce numéro n'a pas
  // de compte Lajan'm » alors que le compte existait.
  it('trouve le destinataire quelle que soit l’écriture du numéro', async () => {
    const { service, ledgerService, users } = buildService();
    await fund(ledgerService, senderAccount.id, 100_000n);

    for (const written of ['+50900000002', '50900000002', '00 509 00000002', '+509 00-00-00-02']) {
      await expect(
        service.transfer(sender.id, {
          recipientPhone: written,
          amountHTG: 1,
          clientRequestId: randomUUID(),
        }),
      ).resolves.toMatchObject({ idempotent: false });
    }

    // Chaque recherche a bien porté sur la forme canonique, une seule.
    const queried = users.findOneBy.mock.calls.map((c: [{ phone: string }]) => c[0].phone);
    expect(new Set(queried)).toEqual(new Set(['+50900000002']));
  });

  // --- Confirmation du destinataire ------------------------------------
  it('renvoie un nom masqué, jamais l’identité complète', async () => {
    const { service, users } = buildService();
    users.findOneBy.mockResolvedValueOnce({
      ...recipient,
      fullName: 'Mirlande Pierre',
      email: 'mirlande@example.com',
    });

    const result = await service.lookupRecipient(sender.id, { phone: '00000002' });

    expect(result).toEqual({ exists: true, displayName: 'Mirlande P.', phone: '+50900000002' });
    expect(JSON.stringify(result)).not.toContain('Pierre');
    expect(JSON.stringify(result)).not.toContain('example.com');
  });

  it('traite son propre numéro comme inconnu', async () => {
    const { service } = buildService();
    await expect(service.lookupRecipient(sender.id, { phone: sender.phone })).resolves.toEqual({
      exists: false,
      displayName: null,
      phone: sender.phone,
    });
  });

  it('compte la recherche avant de toucher la base', async () => {
    const { service, rateLimitService, users } = buildService({
      rateLimitService: { consume: jest.fn().mockRejectedValue(new Error('trop de requêtes')) },
    });

    await expect(service.lookupRecipient(sender.id, { phone: '00000002' })).rejects.toThrow(
      'trop de requêtes',
    );
    // La limite doit être atteinte SANS avoir interrogé l'annuaire :
    // sinon elle ne protège de rien.
    expect(users.findOneBy).not.toHaveBeenCalled();
    expect(rateLimitService.consume).toHaveBeenCalledWith(
      `wallet:lookup:${sender.id}`,
      expect.any(Number),
      expect.any(Number),
    );
  });

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
