import { BadRequestException, Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { PaginationQueryDto, toFindPaging } from '../../common/dto/pagination-query.dto';
import { AuditService } from '../audit/audit.service';
import { User } from '../auth/entities/user.entity';
import { FraudService } from '../fraud/fraud.service';
import { EntryDirection } from '../ledger/entities/ledger-entry.entity';
import { OperationStatus, OperationType } from '../ledger/entities/operation.entity';
import { LedgerService } from '../ledger/ledger.service';
import { AccountsService } from '../ledger/services/accounts.service';
import { NotificationsService } from '../notifications/notifications.service';
import { OtpPurpose } from '../security/entities/otp-code.entity';
import { SecurityService } from '../security/security.service';
import { MONCASH_FLOAT_ACCOUNT } from '../topup/topup.service';
import { MonCashClient, MonCashUnavailableError } from '../topup/moncash-client.service';
import { InitiatePayoutDto } from './dto/initiate-payout.dto';
import { PayoutStatus, PayoutTransaction } from './entities/payout-transaction.entity';

const DEFAULT_MAX_PAYOUT_HTG = 100_000;

@Injectable()
export class PayoutService {
  private readonly logger = new Logger(PayoutService.name);

  constructor(
    @InjectRepository(PayoutTransaction) private readonly transactions: Repository<PayoutTransaction>,
    @InjectRepository(User) private readonly users: Repository<User>,
    private readonly config: ConfigService,
    private readonly ledgerService: LedgerService,
    private readonly accountsService: AccountsService,
    private readonly auditService: AuditService,
    private readonly notificationsService: NotificationsService,
    private readonly monCashClient: MonCashClient,
    private readonly securityService: SecurityService,
    private readonly fraudService: FraudService,
  ) {}

  /**
   * The regulatory per-transaction cap (BRH Circular n°121: 100,000 HTG),
   * configurable via PAYOUT_MAX_AMOUNT_HTG but active by default — never
   * silently unlimited just because the env var is unset. Tier-based caps
   * (basic vs verified, daily/monthly) are layered on top of this by the
   * security module; this is the floor everyone shares regardless of tier.
   */
  getMaxAmountHTG(): number {
    return this.config.get<number>('PAYOUT_MAX_AMOUNT_HTG') || DEFAULT_MAX_PAYOUT_HTG;
  }

  async initiate(
    userId: string,
    dto: InitiatePayoutDto,
  ): Promise<{ payoutTransactionId: string; status: PayoutStatus; failureReason: string | null }> {
    const maxAmount = this.getMaxAmountHTG();
    if (dto.amountHTG > maxAmount) {
      throw new BadRequestException(
        `Payout exceeds the maximum allowed per transaction (${maxAmount} HTG)`,
      );
    }

    const user = await this.users.findOneByOrFail({ id: userId });
    const walletAccount = await this.accountsService.getOrCreateUserWalletAccount(userId);
    const floatAccount = await this.accountsService.getOrCreateSystemAccount(MONCASH_FLOAT_ACCOUNT);
    const amountMinor = BigInt(dto.amountHTG * 100);

    // Best-effort pre-check only — see WalletService.transfer for the same
    // documented race-condition caveat.
    const balance = await this.ledgerService.getBalance(walletAccount.id);
    if (balance < amountMinor) {
      throw new BadRequestException('Insufficient balance');
    }

    await this.securityService.enforceLimits(userId, user.tier, walletAccount.id, dto.amountHTG);
    await this.securityService.enforceOtpIfRequired(
      userId,
      OtpPurpose.PAYOUT,
      dto.amountHTG,
      dto.otpRequestId && dto.otpCode ? { otpRequestId: dto.otpRequestId, code: dto.otpCode } : undefined,
    );

    // Reserve: debit the wallet NOW, before MonCash has confirmed
    // anything, so the funds can't be spent twice while the payout is in
    // flight. The operation is PENDING until confirmed or reversed below
    // — never deleted, only ever confirmed or mirrored-and-reversed (see
    // LedgerService.confirmOperation / reverseOperation).
    const reserve = await this.ledgerService.postOperation({
      idempotencyKey: `payout-reserve:${dto.clientRequestId}`,
      type: OperationType.PAYOUT,
      status: OperationStatus.PENDING,
      entries: [
        { accountId: walletAccount.id, direction: EntryDirection.DEBIT, amountMinor },
        { accountId: floatAccount.id, direction: EntryDirection.CREDIT, amountMinor },
      ],
      metadata: { userId },
    });

    if (reserve.idempotent) {
      const existing = await this.transactions.findOneBy({ operationId: reserve.operation.id });
      if (existing) {
        // Same clientRequestId retried after we already resolved it —
        // return the same outcome rather than calling MonCash again.
        return {
          payoutTransactionId: existing.id,
          status: existing.status,
          failureReason: existing.failureReason,
        };
      }
      // Reserve succeeded on a previous attempt but no PayoutTransaction
      // row was ever written (e.g. a crash between the two writes) — the
      // ledger is left with a PENDING operation with no automatic
      // resolution path yet. Flagged for the Module 9 hardening pass
      // (needs a reconciliation job for stale PENDING payouts); for now
      // this falls through and re-attempts MonCash, which is safe only
      // because MonCash's own idempotency on orderId (=reserve.operation.id)
      // is assumed, not verified — see MonCashClient doc comment.
      this.logger.warn(
        `Payout reserve ${reserve.operation.id} has no PayoutTransaction row — retrying resolution`,
      );
    }

    try {
      const payout = await this.monCashClient.createPayout(reserve.operation.id, dto.amountHTG, user.phone);
      await this.ledgerService.confirmOperation(reserve.operation.id);

      const transaction = await this.transactions.save(
        this.transactions.create({
          userId,
          amountMinor: amountMinor.toString(),
          status: PayoutStatus.COMPLETED,
          operationId: reserve.operation.id,
          providerReference: payout.reference,
        }),
      );

      await this.auditService.record({
        action: 'payout.completed',
        actorId: userId,
        actorType: 'user',
        targetId: transaction.id,
        metadata: { amountHTG: dto.amountHTG },
      });
      await this.notificationsService.notify(userId, {
        type: 'payout.completed',
        title: 'Retrè konplete',
        body: `${dto.amountHTG} HTG voye sou kont MonCash ou`,
      });
      await this.fraudService.evaluate({
        userId,
        accountId: walletAccount.id,
        operationId: reserve.operation.id,
        amountMinor,
      });

      return { payoutTransactionId: transaction.id, status: transaction.status, failureReason: null };
    } catch (error) {
      if (!(error instanceof MonCashUnavailableError)) throw error;

      // Money was already debited above. Since it's already reserved and
      // MonCash can't confirm it, we reverse immediately rather than
      // queueing a retry (unlike top-up, where nothing was debited yet) —
      // holding a customer's funds hostage during a MonCash outage is
      // worse than a failed withdrawal they can safely retry.
      const failureReason = 'MonCash could not complete the payout';
      const reversal = await this.ledgerService.reverseOperation(
        reserve.operation.id,
        `payout-reversal:${dto.clientRequestId}`,
      );

      const transaction = await this.transactions.save(
        this.transactions.create({
          userId,
          amountMinor: amountMinor.toString(),
          status: PayoutStatus.FAILED,
          operationId: reserve.operation.id,
          reversalOperationId: reversal.operation.id,
          failureReason,
        }),
      );

      await this.auditService.record({
        action: 'payout.failed',
        actorId: userId,
        actorType: 'user',
        targetId: transaction.id,
        metadata: { amountHTG: dto.amountHTG, reason: failureReason },
      });
      await this.notificationsService.notify(userId, {
        type: 'payout.completed',
        title: 'Retrè echwe',
        body: `Retrè a echwe. Lajan ou retounen nan kont ou.`,
      });

      return { payoutTransactionId: transaction.id, status: transaction.status, failureReason };
    }
  }

  async history(userId: string, paging?: PaginationQueryDto): Promise<PayoutTransaction[]> {
    return this.transactions.find({
      where: { userId },
      order: { createdAt: 'DESC' },
      ...toFindPaging(paging),
    });
  }
}
