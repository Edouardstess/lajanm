import { BadRequestException, Injectable, NotFoundException } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { User } from '../auth/entities/user.entity';
import { EntryDirection } from '../ledger/entities/ledger-entry.entity';
import { OperationType } from '../ledger/entities/operation.entity';
import { LedgerService } from '../ledger/ledger.service';
import { AccountsService } from '../ledger/services/accounts.service';
import { NotificationsService } from '../notifications/notifications.service';
import { HistoryQueryDto } from './dto/history-query.dto';
import { TransferDto } from './dto/transfer.dto';

export interface BalanceSnapshot {
  balanceMinor: string;
  currency: string;
  // Always the server's clock at query time — the mobile client is
  // responsible for stamping this on its local cache so a later offline
  // read can honestly show "as of <asOf>" instead of implying freshness
  // it doesn't have.
  asOf: string;
}

@Injectable()
export class WalletService {
  constructor(
    @InjectRepository(User) private readonly users: Repository<User>,
    private readonly ledgerService: LedgerService,
    private readonly accountsService: AccountsService,
    private readonly auditService: AuditService,
    private readonly notificationsService: NotificationsService,
  ) {}

  async getBalance(userId: string): Promise<BalanceSnapshot> {
    const account = await this.accountsService.getOrCreateUserWalletAccount(userId);
    const balance = await this.ledgerService.getBalance(account.id);
    return { balanceMinor: balance.toString(), currency: account.currency, asOf: new Date().toISOString() };
  }

  async transfer(senderId: string, dto: TransferDto): Promise<{ operationId: string; idempotent: boolean }> {
    const recipient = await this.users.findOneBy({ phone: dto.recipientPhone });
    if (!recipient) {
      throw new NotFoundException('No Lajan’m account found for this phone number');
    }
    if (recipient.id === senderId) {
      throw new BadRequestException('Cannot transfer to your own account');
    }

    const senderAccount = await this.accountsService.getOrCreateUserWalletAccount(senderId);
    const recipientAccount = await this.accountsService.getOrCreateUserWalletAccount(recipient.id);
    const amountMinor = BigInt(dto.amountHTG * 100);

    // Best-effort pre-check for a fast, friendly error — NOT the source of
    // truth against overdraft. The real guarantee is that postOperation's
    // debit entry can never make the ledger inconsistent; a concurrent
    // transfer racing this check is a known limitation for the MVP (see
    // docs/architecture.md hardening notes) and is revisited alongside the
    // security module's velocity limits.
    const senderBalance = await this.ledgerService.getBalance(senderAccount.id);
    if (senderBalance < amountMinor) {
      throw new BadRequestException('Insufficient balance');
    }

    const result = await this.ledgerService.postOperation({
      idempotencyKey: `wallet-transfer:${dto.clientRequestId}`,
      type: OperationType.TRANSFER,
      entries: [
        { accountId: senderAccount.id, direction: EntryDirection.DEBIT, amountMinor },
        { accountId: recipientAccount.id, direction: EntryDirection.CREDIT, amountMinor },
      ],
      metadata: { senderId, recipientId: recipient.id },
    });

    await this.auditService.record({
      action: 'wallet.transfer',
      actorId: senderId,
      actorType: 'user',
      targetId: result.operation.id,
      metadata: { recipientId: recipient.id, amountHTG: dto.amountHTG, idempotent: result.idempotent },
    });

    if (!result.idempotent) {
      await this.notificationsService.notify(senderId, {
        type: 'wallet.debit',
        title: 'Lajan voye',
        body: `Ou voye ${dto.amountHTG} HTG`,
      });
      await this.notificationsService.notify(recipient.id, {
        type: 'wallet.credit',
        title: 'Lajan resevwa',
        body: `Ou resevwa ${dto.amountHTG} HTG`,
      });
    }

    return { operationId: result.operation.id, idempotent: result.idempotent };
  }

  async history(userId: string, query: HistoryQueryDto) {
    const account = await this.accountsService.getOrCreateUserWalletAccount(userId);
    const entries = await this.ledgerService.listEntries(account.id, {
      limit: query.limit,
      offset: query.offset,
      from: query.from ? new Date(query.from) : undefined,
      to: query.to ? new Date(query.to) : undefined,
      types: query.type ? [query.type] : undefined,
    });

    return entries.map((entry) => ({
      id: entry.id,
      operationType: entry.operationType,
      direction: entry.direction,
      amountMinor: entry.amountMinor,
      currency: entry.currency,
      createdAt: entry.createdAt,
    }));
  }
}
