import { BadRequestException, Injectable, NotFoundException } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { normalizePhone } from '../../common/phone';
import { RateLimitService } from '../../common/rate-limit.service';
import { AuditService } from '../audit/audit.service';
import { User } from '../auth/entities/user.entity';
import { FraudService } from '../fraud/fraud.service';
import { EntryDirection } from '../ledger/entities/ledger-entry.entity';
import { OperationType } from '../ledger/entities/operation.entity';
import { LedgerService } from '../ledger/ledger.service';
import { AccountsService } from '../ledger/services/accounts.service';
import { NotificationsService } from '../notifications/notifications.service';
import { OtpPurpose } from '../security/entities/otp-code.entity';
import { SecurityService } from '../security/security.service';
import { HistoryQueryDto } from './dto/history-query.dto';
import { LookupRecipientDto } from './dto/lookup-recipient.dto';
import { TransferDto } from './dto/transfer.dto';

// 30 recherches par heure : largement au-dessus d'un usage normal
// (on envoie de l'argent à quelques personnes), largement en dessous de
// ce qu'il faut pour balayer un annuaire.
const LOOKUP_LIMIT = 30;
const LOOKUP_WINDOW_SECONDS = 3600;

/**
 * « Mirlande Pierre » -> « Mirlande P. ». Assez pour reconnaître
 * quelqu'un qu'on connaît, trop peu pour identifier un inconnu.
 */
function maskName(fullName: string | null): string | null {
  const parts = (fullName ?? '').trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return null;
  if (parts.length === 1) return parts[0];
  return `${parts[0]} ${parts[parts.length - 1][0].toUpperCase()}.`;
}

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
    private readonly securityService: SecurityService,
    private readonly fraudService: FraudService,
    private readonly rateLimitService: RateLimitService,
  ) {}

  async getBalance(userId: string): Promise<BalanceSnapshot> {
    const account = await this.accountsService.getOrCreateUserWalletAccount(userId);
    const balance = await this.ledgerService.getBalance(account.id);
    return { balanceMinor: balance.toString(), currency: account.currency, asOf: new Date().toISOString() };
  }

  async transfer(senderId: string, dto: TransferDto): Promise<{ operationId: string; idempotent: boolean }> {
    const sender = await this.users.findOneByOrFail({ id: senderId });
    const recipient = await this.users.findOneBy({ phone: normalizePhone(dto.recipientPhone) });
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
    // docs/architecture.md hardening notes).
    const senderBalance = await this.ledgerService.getBalance(senderAccount.id);
    if (senderBalance < amountMinor) {
      throw new BadRequestException('Insufficient balance');
    }

    await this.securityService.enforceLimits(senderId, sender.tier, senderAccount.id, dto.amountHTG);
    await this.securityService.enforceOtpIfRequired(
      senderId,
      OtpPurpose.TRANSFER,
      dto.amountHTG,
      dto.otpRequestId && dto.otpCode ? { otpRequestId: dto.otpRequestId, code: dto.otpCode } : undefined,
    );

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
      await this.fraudService.evaluate({
        userId: senderId,
        accountId: senderAccount.id,
        operationId: result.operation.id,
        amountMinor,
        recipientId: recipient.id,
      });
    }

    return { operationId: result.operation.id, idempotent: result.idempotent };
  }

  /**
   * Dit à qui un numéro appartient, avant l'envoi.
   *
   * Un transfert est irréversible : sans cette étape, un chiffre de trop
   * envoie l'argent chez un inconnu, et l'expéditeur ne l'apprend
   * qu'après. C'est la raison d'être de cet endpoint.
   *
   * Il a une contrepartie qu'il faut nommer : il révèle qu'un numéro a un
   * compte Lajan'm, donc il permet de tester un annuaire. Trois garde-fous,
   * et ils ne suppriment pas le risque, ils le bornent :
   *   - authentification obligatoire (pas d'accès anonyme) ;
   *   - 30 recherches par heure et par compte, comptées dans Redis ;
   *   - le nom renvoyé est masqué (« Mirlande P. »), jamais l'identité
   *     complète, jamais l'e-mail, jamais le niveau de compte.
   *
   * Le nom masqué suffit à reconnaître son destinataire quand on le
   * connaît, et n'apprend presque rien à qui ne le connaît pas.
   */
  async lookupRecipient(
    senderId: string,
    dto: LookupRecipientDto,
  ): Promise<{ exists: boolean; displayName: string | null; phone: string }> {
    // La limite est comptée AVANT la normalisation : sinon quatre
    // écritures du même numéro compteraient pour quatre recherches
    // différentes tout en interrogeant la même ligne.
    await this.rateLimitService.consume(`wallet:lookup:${senderId}`, LOOKUP_LIMIT, LOOKUP_WINDOW_SECONDS);

    const phone = normalizePhone(dto.phone);
    const recipient = await this.users.findOneBy({ phone });

    if (!recipient || recipient.id === senderId) {
      // Son propre numéro est traité comme inexistant : l'envoi à soi-même
      // est refusé plus loin de toute façon, et confirmer « c'est vous »
      // n'apporte rien.
      return { exists: false, displayName: null, phone };
    }

    return { exists: true, displayName: maskName(recipient.fullName), phone };
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
