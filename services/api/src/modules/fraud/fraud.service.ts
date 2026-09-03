import { Injectable, Logger, NotFoundException } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { InjectDataSource, InjectRepository } from '@nestjs/typeorm';
import { DataSource, Repository } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { EntryDirection } from '../ledger/entities/ledger-entry.entity';
import { LedgerService } from '../ledger/ledger.service';
import { ResolveFlagDto } from './dto/resolve-flag.dto';
import { FraudFlag, FraudFlagStatus, FraudRuleCode } from './entities/fraud-flag.entity';

const DEFAULT_VELOCITY_COUNT_THRESHOLD = 5;
const DEFAULT_VELOCITY_WINDOW_MINUTES = 10;
const DEFAULT_AMOUNT_MULTIPLIER = 5;
const DEFAULT_NEW_BENEFICIARY_THRESHOLD = 3;
const MIN_HISTORY_FOR_AMOUNT_CHECK = 5;

export interface FraudEvaluationContext {
  userId: string;
  accountId: string;
  operationId: string;
  amountMinor: bigint;
  /** Only set for transfers — drives the "frequent new beneficiaries" rule. */
  recipientId?: string;
}

/**
 * Velocity-rule fraud detection. Every rule here only ever creates a
 * FraudFlag (status OPEN) for a human to triage in the compliance
 * back-office — nothing in this service blocks or reverses a
 * transaction. See docs/architecture.md on why: false positives on a
 * payments product are expensive, so the product decision is
 * flag-and-review, not auto-block.
 */
@Injectable()
export class FraudService {
  private readonly logger = new Logger(FraudService.name);

  constructor(
    @InjectRepository(FraudFlag) private readonly flags: Repository<FraudFlag>,
    @InjectDataSource() private readonly dataSource: DataSource,
    private readonly config: ConfigService,
    private readonly ledgerService: LedgerService,
    private readonly auditService: AuditService,
  ) {}

  /**
   * Runs every rule for one just-completed debit. Deliberately swallows
   * its own errors after logging — a bug in fraud detection must never
   * fail the underlying financial transaction it's evaluating.
   */
  async evaluate(context: FraudEvaluationContext): Promise<void> {
    try {
      await Promise.all([
        this.checkVelocity(context),
        this.checkAmountAnomaly(context),
        context.recipientId ? this.checkFrequentNewBeneficiaries(context) : Promise.resolve(),
      ]);
    } catch (error) {
      this.logger.error(`Fraud evaluation failed for operation ${context.operationId}: ${String(error)}`);
    }
  }

  async listOpenFlags(): Promise<FraudFlag[]> {
    return this.flags.find({ where: { status: FraudFlagStatus.OPEN }, order: { createdAt: 'DESC' } });
  }

  async resolve(flagId: string, resolverId: string, dto: ResolveFlagDto): Promise<FraudFlag> {
    const flag = await this.flags.findOneBy({ id: flagId });
    if (!flag) throw new NotFoundException('Fraud flag not found');

    flag.status = dto.status;
    flag.resolvedBy = resolverId;
    flag.resolvedAt = new Date();
    await this.flags.save(flag);

    await this.auditService.record({
      action: 'fraud.flag_resolved',
      actorId: resolverId,
      actorType: 'admin',
      targetId: flag.id,
      metadata: { status: dto.status },
    });

    return flag;
  }

  private async checkVelocity(context: FraudEvaluationContext): Promise<void> {
    const windowMinutes =
      this.config.get<number>('FRAUD_VELOCITY_WINDOW_MINUTES') || DEFAULT_VELOCITY_WINDOW_MINUTES;
    const threshold =
      this.config.get<number>('FRAUD_VELOCITY_COUNT_THRESHOLD') || DEFAULT_VELOCITY_COUNT_THRESHOLD;
    const since = new Date(Date.now() - windowMinutes * 60 * 1000);

    const count = await this.ledgerService.countDirection(context.accountId, EntryDirection.DEBIT, since);
    if (count >= threshold) {
      await this.raise(context, FraudRuleCode.HIGH_VELOCITY, { count, windowMinutes, threshold });
    }
  }

  private async checkAmountAnomaly(context: FraudEvaluationContext): Promise<void> {
    const multiplier = this.config.get<number>('FRAUD_AMOUNT_MULTIPLIER') || DEFAULT_AMOUNT_MULTIPLIER;
    // A wide lookback window is fine here — this reads recent history to
    // establish a baseline, not to enforce a cap.
    const since = new Date(Date.now() - 90 * 24 * 60 * 60 * 1000);
    const entries = await this.ledgerService.listEntries(context.accountId, { limit: 25, from: since });
    const priorDebits = entries
      .filter((e) => e.direction === EntryDirection.DEBIT && e.operationId !== context.operationId)
      .map((e) => BigInt(e.amountMinor));

    if (priorDebits.length < MIN_HISTORY_FOR_AMOUNT_CHECK) return;

    const average = priorDebits.reduce((sum, v) => sum + v, 0n) / BigInt(priorDebits.length);
    if (average > 0n && context.amountMinor > average * BigInt(multiplier)) {
      await this.raise(context, FraudRuleCode.AMOUNT_ANOMALY, {
        amountMinor: context.amountMinor.toString(),
        averageMinor: average.toString(),
        multiplier,
      });
    }
  }

  private async checkFrequentNewBeneficiaries(context: FraudEvaluationContext): Promise<void> {
    const threshold =
      this.config.get<number>('FRAUD_NEW_BENEFICIARY_THRESHOLD') || DEFAULT_NEW_BENEFICIARY_THRESHOLD;
    const since = new Date(Date.now() - 24 * 60 * 60 * 1000);

    // Raw query: distinct transfer recipients for this sender in the
    // window. Parameterized ($1/$2), not string-interpolated — userId
    // and since are the only inputs and both come from server-derived
    // context, never raw client input, but parameterizing costs nothing
    // and removes any question about it.
    const rows = await this.dataSource.query<Array<{ count: string }>>(
      `SELECT COUNT(DISTINCT metadata->>'recipientId') AS count
       FROM operations
       WHERE type = 'transfer' AND metadata->>'senderId' = $1 AND "createdAt" >= $2`,
      [context.userId, since],
    );
    const distinctRecipients = Number(rows[0]?.count ?? 0);

    if (distinctRecipients >= threshold) {
      await this.raise(context, FraudRuleCode.FREQUENT_NEW_BENEFICIARIES, {
        distinctRecipients,
        threshold,
        windowHours: 24,
      });
    }
  }

  private async raise(
    context: FraudEvaluationContext,
    ruleCode: FraudRuleCode,
    details: Record<string, unknown>,
  ): Promise<void> {
    const flag = await this.flags.save(
      this.flags.create({
        userId: context.userId,
        ruleCode,
        relatedOperationId: context.operationId,
        details,
      }),
    );

    await this.auditService.record({
      action: 'fraud.flag_raised',
      actorId: 'system',
      actorType: 'system',
      targetId: flag.id,
      metadata: { ruleCode, userId: context.userId, ...details },
    });
  }
}
