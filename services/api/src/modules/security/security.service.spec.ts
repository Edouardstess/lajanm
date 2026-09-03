import { BadRequestException, UnauthorizedException } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { randomUUID } from 'crypto';
import { DataSource } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { User, UserTier } from '../auth/entities/user.entity';
import { LedgerService } from '../ledger/ledger.service';
import { FakeLedgerDataSource } from '../../testing/fake-ledger-data-source';
import { OtpCode, OtpPurpose } from './entities/otp-code.entity';
import { SecurityService } from './security.service';
import { SmsService } from './sms.service';

const testUser: User = { id: 'user-1', phone: '+50900000001' } as User;

function createOtpRepo() {
  const rows: OtpCode[] = [];
  return {
    rows,
    create: jest.fn(
      (data: Partial<OtpCode>) =>
        ({ id: randomUUID(), attempts: 0, maxAttempts: 3, consumedAt: null, createdAt: new Date(), ...data }) as OtpCode,
    ),
    save: jest.fn(async (entity: OtpCode) => {
      const i = rows.findIndex((r) => r.id === entity.id);
      if (i === -1) rows.push(entity);
      else rows[i] = entity;
      return entity;
    }),
    findOneBy: jest.fn(async (where: Partial<OtpCode>) => {
      const keys = Object.keys(where) as Array<keyof OtpCode>;
      return rows.find((r) => keys.every((k) => r[k] === where[k])) ?? null;
    }),
  };
}

function buildService(configValues: Record<string, unknown> = {}) {
  const otpCodes = createOtpRepo();
  const users = { findOneByOrFail: jest.fn().mockResolvedValue(testUser) };
  const config = new ConfigService({ JWT_SECRET: 'test-secret', ...configValues });
  const smsService = { sendOtp: jest.fn().mockResolvedValue(undefined) } as unknown as SmsService;
  const fakeLedgerDataSource = new FakeLedgerDataSource();
  const ledgerService = new LedgerService(fakeLedgerDataSource as unknown as DataSource);
  const auditService = { record: jest.fn().mockResolvedValue(undefined) } as unknown as AuditService;

  const service = new SecurityService(
    otpCodes as unknown as never,
    users as unknown as never,
    config,
    smsService,
    ledgerService,
    auditService,
  );

  return { service, otpCodes, smsService, ledgerService };
}

describe('SecurityService OTP', () => {
  it('is a no-op below the threshold', async () => {
    const { service } = buildService({ OTP_THRESHOLD_HTG: 10_000 });
    await expect(
      service.enforceOtpIfRequired('user-1', OtpPurpose.TRANSFER, 5_000),
    ).resolves.toBeUndefined();
  });

  it('requires an OTP at/above the threshold', async () => {
    const { service } = buildService({ OTP_THRESHOLD_HTG: 10_000 });
    await expect(
      service.enforceOtpIfRequired('user-1', OtpPurpose.TRANSFER, 10_000),
    ).rejects.toThrow(BadRequestException);
  });

  it('accepts the correct code exactly once, then rejects reuse', async () => {
    const { service, smsService } = buildService({ OTP_THRESHOLD_HTG: 10_000 });
    const { otpRequestId } = await service.requestOtp('user-1', OtpPurpose.TRANSFER);

    // Only the hash is ever stored (see SecurityService.hashCode's doc) —
    // the plaintext code never round-trips through the service, so the
    // only place to observe it is where it's "sent", same as a real SMS
    // delivery test would.
    const [, sentCode] = (smsService.sendOtp as jest.Mock).mock.calls[0];

    await expect(
      service.enforceOtpIfRequired('user-1', OtpPurpose.TRANSFER, 10_000, {
        otpRequestId,
        code: sentCode,
      }),
    ).resolves.toBeUndefined();

    // Reusing a consumed OTP must fail even with the correct code.
    await expect(service.verifyAndConsumeOtp('user-1', otpRequestId, sentCode)).rejects.toThrow(
      'already been used',
    );
  });

  it('rejects a wrong code without consuming a correct future attempt', async () => {
    const { service } = buildService({ OTP_THRESHOLD_HTG: 10_000 });
    const { otpRequestId } = await service.requestOtp('user-1', OtpPurpose.TRANSFER);

    await expect(service.verifyAndConsumeOtp('user-1', otpRequestId, '000000')).rejects.toThrow(
      UnauthorizedException,
    );
  });

  it('locks out after maxAttempts wrong codes', async () => {
    const { service } = buildService({ OTP_THRESHOLD_HTG: 10_000 });
    const { otpRequestId } = await service.requestOtp('user-1', OtpPurpose.TRANSFER);

    for (let i = 0; i < 3; i += 1) {
      await expect(
        service.verifyAndConsumeOtp('user-1', otpRequestId, 'wrong-' + i),
      ).rejects.toThrow(UnauthorizedException);
    }

    await expect(service.verifyAndConsumeOtp('user-1', otpRequestId, 'wrong-again')).rejects.toThrow(
      'Too many incorrect attempts',
    );
  });

  it('rejects a code from a different user than the one who requested it', async () => {
    const { service } = buildService({ OTP_THRESHOLD_HTG: 10_000 });
    const { otpRequestId } = await service.requestOtp('user-1', OtpPurpose.TRANSFER);

    await expect(service.verifyAndConsumeOtp('someone-else', otpRequestId, '123456')).rejects.toThrow(
      'Invalid OTP request',
    );
  });
});

describe('SecurityService.enforceLimits', () => {
  it('allows a transaction within both daily and monthly limits', async () => {
    const { service } = buildService({ LIMIT_BASIC_DAILY_HTG: 25_000, LIMIT_BASIC_MONTHLY_HTG: 100_000 });
    await expect(
      service.enforceLimits('user-1', UserTier.BASIC, 'acc-1', 1_000),
    ).resolves.toBeUndefined();
  });

  it('rejects a transaction that would exceed the daily limit', async () => {
    const { service, ledgerService } = buildService({ LIMIT_BASIC_DAILY_HTG: 1_000 });
    await ledgerService.postOperation({
      idempotencyKey: 'seed-1',
      type: 'transfer' as never,
      entries: [
        { accountId: 'acc-1', direction: 'debit' as never, amountMinor: 90_000n },
        { accountId: 'other', direction: 'credit' as never, amountMinor: 90_000n },
      ],
    });

    await expect(service.enforceLimits('user-1', UserTier.BASIC, 'acc-1', 500)).rejects.toThrow(
      'daily limit',
    );
  });

  it('gives verified users a higher limit than basic users', async () => {
    const { service, ledgerService } = buildService({
      LIMIT_BASIC_DAILY_HTG: 1_000,
      LIMIT_VERIFIED_DAILY_HTG: 50_000,
    });
    await ledgerService.postOperation({
      idempotencyKey: 'seed-2',
      type: 'transfer' as never,
      entries: [
        { accountId: 'acc-1', direction: 'debit' as never, amountMinor: 90_000n },
        { accountId: 'other', direction: 'credit' as never, amountMinor: 90_000n },
      ],
    });

    await expect(service.enforceLimits('user-1', UserTier.BASIC, 'acc-1', 500)).rejects.toThrow(
      'daily limit',
    );
    await expect(
      service.enforceLimits('user-1', UserTier.VERIFIED, 'acc-1', 500),
    ).resolves.toBeUndefined();
  });
});
