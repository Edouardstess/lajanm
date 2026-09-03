import { Injectable, Logger, NotFoundException, UnauthorizedException } from '@nestjs/common';
import { InjectQueue } from '@nestjs/bullmq';
import { InjectRepository } from '@nestjs/typeorm';
import { Queue } from 'bullmq';
import { Repository } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { EntryDirection } from '../ledger/entities/ledger-entry.entity';
import { OperationType } from '../ledger/entities/operation.entity';
import { AccountsService } from '../ledger/services/accounts.service';
import { LedgerService } from '../ledger/ledger.service';
import { InitiateTopupDto } from './dto/initiate-topup.dto';
import { TopupSource, TopupStatus, TopupTransaction } from './entities/topup-transaction.entity';
import { MonCashClient, MonCashUnavailableError } from './moncash-client.service';

export const TOPUP_INITIATION_QUEUE = 'topup-initiation';
export const MONCASH_FLOAT_ACCOUNT = 'moncash_float';

@Injectable()
export class TopupService {
  private readonly logger = new Logger(TopupService.name);

  constructor(
    @InjectRepository(TopupTransaction) private readonly transactions: Repository<TopupTransaction>,
    @InjectQueue(TOPUP_INITIATION_QUEUE) private readonly retryQueue: Queue,
    private readonly monCashClient: MonCashClient,
    private readonly ledgerService: LedgerService,
    private readonly accountsService: AccountsService,
    private readonly auditService: AuditService,
  ) {}

  async initiate(
    userId: string,
    dto: InitiateTopupDto,
  ): Promise<{ transactionId: string; status: TopupStatus; gatewayUrl: string | null }> {
    const transaction = await this.transactions.save(
      this.transactions.create({
        userId,
        source: TopupSource.MONCASH,
        amountMinor: String(dto.amountHTG * 100),
        status: TopupStatus.PENDING,
      }),
    );

    await this.auditService.record({
      action: 'topup.initiated',
      actorId: userId,
      actorType: 'user',
      targetId: transaction.id,
      metadata: { amountHTG: dto.amountHTG },
    });

    try {
      const payment = await this.monCashClient.createPayment(transaction.id, dto.amountHTG);
      transaction.providerReference = payment.reference;
      await this.transactions.save(transaction);
      return { transactionId: transaction.id, status: transaction.status, gatewayUrl: payment.gatewayUrl };
    } catch (error) {
      if (!(error instanceof MonCashUnavailableError)) throw error;

      // Never report success we don't have. The transaction stays PENDING
      // — honestly reflecting "we don't know yet" — while a background
      // job retries reaching MonCash with backoff.
      await this.retryQueue.add(
        'retry-initiation',
        { transactionId: transaction.id, amountHTG: dto.amountHTG },
        { attempts: 5, backoff: { type: 'exponential', delay: 5_000 } },
      );
      this.logger.warn(`Queued top-up ${transaction.id} for retry: MonCash unavailable`);
      return { transactionId: transaction.id, status: transaction.status, gatewayUrl: null };
    }
  }

  /** Invoked by the BullMQ processor. Not exposed over HTTP. */
  async retryInitiation(transactionId: string, amountHTG: number, isFinalAttempt: boolean): Promise<void> {
    const transaction = await this.transactions.findOneBy({ id: transactionId });
    if (!transaction || transaction.status !== TopupStatus.PENDING || transaction.providerReference) {
      return; // Already resolved (completed/failed) or already has a reference.
    }

    try {
      const payment = await this.monCashClient.createPayment(transaction.id, amountHTG);
      transaction.providerReference = payment.reference;
      await this.transactions.save(transaction);
    } catch (error) {
      if (!isFinalAttempt) throw error; // let BullMQ retry with backoff

      transaction.status = TopupStatus.FAILED;
      transaction.failureReason = 'MonCash unreachable after all retries';
      await this.transactions.save(transaction);
      await this.auditService.record({
        action: 'topup.failed',
        actorId: 'system',
        actorType: 'system',
        targetId: transaction.id,
        metadata: { reason: transaction.failureReason },
      });
    }
  }

  /**
   * Credits the ledger from a verified MonCash webhook. Safe to call
   * multiple times for the same reference — see docs/topup.md — because
   * the idempotency key handed to LedgerService is the MonCash reference
   * itself, and MonCash is known to redeliver webhooks.
   */
  async handleWebhook(rawBody: string, signatureHeader: string | undefined): Promise<void> {
    if (!this.monCashClient.verifyWebhookSignature(rawBody, signatureHeader)) {
      throw new UnauthorizedException('Invalid webhook signature');
    }

    const payload = JSON.parse(rawBody) as { reference: string; status: 'completed' | 'failed' };
    const transaction = await this.transactions.findOneBy({ providerReference: payload.reference });
    if (!transaction) {
      this.logger.warn(`Webhook for unknown MonCash reference ${payload.reference}`);
      return;
    }

    if (payload.status === 'failed') {
      transaction.status = TopupStatus.FAILED;
      transaction.failureReason = 'Payment failed on MonCash side';
      await this.transactions.save(transaction);
      return;
    }

    const walletAccount = await this.accountsService.getOrCreateUserWalletAccount(transaction.userId);
    const floatAccount = await this.accountsService.getOrCreateSystemAccount(MONCASH_FLOAT_ACCOUNT);

    const result = await this.ledgerService.postOperation({
      idempotencyKey: payload.reference,
      type: OperationType.TOPUP,
      entries: [
        { accountId: walletAccount.id, direction: EntryDirection.CREDIT, amountMinor: BigInt(transaction.amountMinor) },
        { accountId: floatAccount.id, direction: EntryDirection.DEBIT, amountMinor: BigInt(transaction.amountMinor) },
      ],
      metadata: { topupTransactionId: transaction.id },
    });

    transaction.status = TopupStatus.COMPLETED;
    transaction.operationId = result.operation.id;
    await this.transactions.save(transaction);

    await this.auditService.record({
      action: 'topup.completed',
      actorId: 'system',
      actorType: 'system',
      targetId: transaction.id,
      metadata: { idempotent: result.idempotent, operationId: result.operation.id },
    });
  }

  async history(userId: string): Promise<TopupTransaction[]> {
    return this.transactions.find({ where: { userId }, order: { createdAt: 'DESC' } });
  }

  async getStatus(userId: string, transactionId: string): Promise<TopupTransaction> {
    const transaction = await this.transactions.findOneBy({ id: transactionId, userId });
    if (!transaction) throw new NotFoundException('Top-up transaction not found');
    return transaction;
  }
}
