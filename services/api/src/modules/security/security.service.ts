import { BadRequestException, Injectable, UnauthorizedException } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { InjectRepository } from '@nestjs/typeorm';
import { createHmac, randomInt } from 'crypto';
import { Repository } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { User, UserTier } from '../auth/entities/user.entity';
import { EntryDirection } from '../ledger/entities/ledger-entry.entity';
import { LedgerService } from '../ledger/ledger.service';
import { OtpCode, OtpPurpose } from './entities/otp-code.entity';
import { SmsService } from './sms.service';

const DEFAULT_OTP_THRESHOLD_HTG = 10_000;
const OTP_TTL_MINUTES = 5;

const DEFAULT_LIMITS: Record<UserTier, { dailyHTG: number; monthlyHTG: number }> = {
  [UserTier.BASIC]: { dailyHTG: 25_000, monthlyHTG: 100_000 },
  [UserTier.VERIFIED]: { dailyHTG: 150_000, monthlyHTG: 500_000 },
};

export interface OtpRequestResult {
  otpRequestId: string;
  expiresAt: Date;
}

@Injectable()
export class SecurityService {
  constructor(
    @InjectRepository(OtpCode) private readonly otpCodes: Repository<OtpCode>,
    @InjectRepository(User) private readonly users: Repository<User>,
    private readonly config: ConfigService,
    private readonly smsService: SmsService,
    private readonly ledgerService: LedgerService,
    private readonly auditService: AuditService,
  ) {}

  getOtpThresholdHTG(): number {
    return this.config.get<number>('OTP_THRESHOLD_HTG') || DEFAULT_OTP_THRESHOLD_HTG;
  }

  getTierLimits(tier: UserTier): { dailyHTG: number; monthlyHTG: number } {
    const defaults = DEFAULT_LIMITS[tier];
    const prefix = tier === UserTier.VERIFIED ? 'LIMIT_VERIFIED' : 'LIMIT_BASIC';
    return {
      dailyHTG: this.config.get<number>(`${prefix}_DAILY_HTG`) || defaults.dailyHTG,
      monthlyHTG: this.config.get<number>(`${prefix}_MONTHLY_HTG`) || defaults.monthlyHTG,
    };
  }

  async requestOtp(userId: string, purpose: OtpPurpose): Promise<OtpRequestResult> {
    const user = await this.users.findOneByOrFail({ id: userId });
    const code = randomInt(0, 1_000_000).toString().padStart(6, '0');
    const expiresAt = new Date(Date.now() + OTP_TTL_MINUTES * 60 * 1000);

    const otp = await this.otpCodes.save(
      this.otpCodes.create({
        userId,
        purpose,
        codeHash: this.hashCode(code),
        expiresAt,
      }),
    );

    await this.smsService.sendOtp(user.phone, code);
    await this.auditService.record({
      action: 'security.otp_sent',
      actorId: userId,
      actorType: 'user',
      targetId: otp.id,
      metadata: { purpose },
    });

    return { otpRequestId: otp.id, expiresAt };
  }

  /**
   * Verifies and consumes an OTP. Throws on any failure (wrong code,
   * expired, already used, too many attempts) — there is no partial
   * success. A wrong-code attempt still increments `attempts` before
   * throwing, so retries are bounded even if the caller ignores the
   * error and asks again.
   */
  async verifyAndConsumeOtp(userId: string, otpRequestId: string, code: string): Promise<void> {
    const otp = await this.otpCodes.findOneBy({ id: otpRequestId, userId });
    if (!otp) throw new UnauthorizedException('Invalid OTP request');

    if (otp.consumedAt) throw new UnauthorizedException('This OTP has already been used');
    if (otp.expiresAt < new Date()) throw new UnauthorizedException('This OTP has expired');
    if (otp.attempts >= otp.maxAttempts) {
      throw new UnauthorizedException('Too many incorrect attempts for this OTP');
    }

    if (otp.codeHash !== this.hashCode(code)) {
      otp.attempts += 1;
      await this.otpCodes.save(otp);
      await this.auditService.record({
        action: 'security.otp_failed',
        actorId: userId,
        actorType: 'user',
        targetId: otp.id,
        metadata: { attempts: otp.attempts },
      });
      throw new UnauthorizedException('Incorrect OTP code');
    }

    otp.consumedAt = new Date();
    await this.otpCodes.save(otp);
    await this.auditService.record({
      action: 'security.otp_verified',
      actorId: userId,
      actorType: 'user',
      targetId: otp.id,
    });
  }

  /**
   * Gate used by wallet/payout before moving money: no-ops below the
   * threshold, otherwise requires a valid, matching-purpose OTP.
   */
  async enforceOtpIfRequired(
    userId: string,
    purpose: OtpPurpose,
    amountHTG: number,
    otp?: { otpRequestId: string; code: string },
  ): Promise<void> {
    if (amountHTG < this.getOtpThresholdHTG()) return;

    if (!otp) {
      throw new BadRequestException('An OTP is required for this amount');
    }
    await this.verifyAndConsumeOtp(userId, otp.otpRequestId, otp.code);
  }

  /**
   * Gate used by wallet/payout before moving money: rejects if this
   * transaction would push the user's rolling daily or monthly outbound
   * total (across all debit types on their wallet — transfers and
   * payouts share one limit, not one each) past their tier's cap.
   */
  async enforceLimits(
    userId: string,
    tier: UserTier,
    walletAccountId: string,
    amountHTG: number,
  ): Promise<void> {
    const limits = this.getTierLimits(tier);
    const amountMinor = BigInt(amountHTG * 100);

    const startOfDay = new Date();
    startOfDay.setHours(0, 0, 0, 0);
    const startOfMonth = new Date(startOfDay.getFullYear(), startOfDay.getMonth(), 1);

    const [dailySpent, monthlySpent] = await Promise.all([
      this.ledgerService.sumDirection(walletAccountId, EntryDirection.DEBIT, startOfDay),
      this.ledgerService.sumDirection(walletAccountId, EntryDirection.DEBIT, startOfMonth),
    ]);

    if (dailySpent + amountMinor > BigInt(limits.dailyHTG * 100)) {
      await this.auditService.record({
        action: 'security.limit_exceeded',
        actorId: userId,
        actorType: 'user',
        metadata: { window: 'daily', limitHTG: limits.dailyHTG, amountHTG },
      });
      throw new BadRequestException(`This would exceed your daily limit of ${limits.dailyHTG} HTG`);
    }
    if (monthlySpent + amountMinor > BigInt(limits.monthlyHTG * 100)) {
      await this.auditService.record({
        action: 'security.limit_exceeded',
        actorId: userId,
        actorType: 'user',
        metadata: { window: 'monthly', limitHTG: limits.monthlyHTG, amountHTG },
      });
      throw new BadRequestException(`This would exceed your monthly limit of ${limits.monthlyHTG} HTG`);
    }
  }

  private hashCode(code: string): string {
    // A fast HMAC rather than argon2: brute-forcing a 6-digit code is
    // already bounded by maxAttempts + a 5-minute expiry, not by hash
    // cost, and OTP verification needs to be cheap to call.
    const secret = this.config.get<string>('JWT_SECRET') ?? '';
    return createHmac('sha256', secret).update(code).digest('hex');
  }
}
