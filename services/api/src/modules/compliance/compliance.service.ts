import { Injectable, NotFoundException } from '@nestjs/common';
import { InjectDataSource, InjectRepository } from '@nestjs/typeorm';
import { DataSource, Repository } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { AccountsService } from '../ledger/services/accounts.service';
import { LedgerService } from '../ledger/ledger.service';
import { MONCASH_FLOAT_ACCOUNT } from '../topup/topup.service';
import { CreateDisputeDto } from './dto/create-dispute.dto';
import { CreateSarDto } from './dto/create-sar.dto';
import { UpdateDisputeDto } from './dto/update-dispute.dto';
import { Dispute } from './entities/dispute.entity';
import { SuspiciousActivityReport } from './entities/suspicious-activity-report.entity';

export interface ReconciliationReport {
  internalWalletTotalMinor: string;
  moncashFloatBalanceMinor: string;
  discrepancyMinor: string;
  isBalanced: boolean;
  note: string;
}

@Injectable()
export class ComplianceService {
  constructor(
    @InjectRepository(Dispute) private readonly disputes: Repository<Dispute>,
    @InjectRepository(SuspiciousActivityReport) private readonly sars: Repository<SuspiciousActivityReport>,
    @InjectDataSource() private readonly dataSource: DataSource,
    private readonly ledgerService: LedgerService,
    private readonly accountsService: AccountsService,
    private readonly auditService: AuditService,
  ) {}

  /**
   * Internal reconciliation only: sums every user wallet balance and
   * compares it to the moncash_float system account, which the ledger
   * design guarantees sum to zero in a consistent system (a top-up credits
   * a wallet and debits the float by the same amount; a payout does the
   * reverse — see docs/architecture.md). This does NOT call MonCash's own
   * balance API (no integration exists — see MonCashClient's doc comment)
   * so it cannot catch a discrepancy between our records and MonCash's
   * actual float; it only catches internal ledger inconsistencies.
   */
  async getReconciliation(): Promise<ReconciliationReport> {
    const walletTotalRow = await this.dataSource.query<Array<{ total: string }>>(`
      SELECT COALESCE(SUM(CASE WHEN le.direction = 'credit' THEN le."amountMinor" ELSE -le."amountMinor" END), 0) AS total
      FROM ledger_entries le
      JOIN accounts a ON a.id = le."accountId"
      WHERE a."ownerType" = 'user' AND a.name = 'wallet'
    `);
    const internalWalletTotalMinor = BigInt(walletTotalRow[0]?.total ?? '0');

    const floatAccount = await this.accountsService.getOrCreateSystemAccount(MONCASH_FLOAT_ACCOUNT);
    const moncashFloatBalanceMinor = await this.ledgerService.getBalance(floatAccount.id);

    const discrepancyMinor = internalWalletTotalMinor + moncashFloatBalanceMinor;

    return {
      internalWalletTotalMinor: internalWalletTotalMinor.toString(),
      moncashFloatBalanceMinor: moncashFloatBalanceMinor.toString(),
      discrepancyMinor: discrepancyMinor.toString(),
      isBalanced: discrepancyMinor === 0n,
      note: 'Internal reconciliation only — no MonCash balance API integration exists to compare against the real external float.',
    };
  }

  async createDispute(userId: string, dto: CreateDisputeDto): Promise<Dispute> {
    const dispute = await this.disputes.save(
      this.disputes.create({
        userId,
        subject: dto.subject,
        description: dto.description,
        relatedOperationId: dto.relatedOperationId ?? null,
      }),
    );

    await this.auditService.record({
      action: 'compliance.dispute_created',
      actorId: userId,
      actorType: 'user',
      targetId: dispute.id,
    });

    return dispute;
  }

  async listMyDisputes(userId: string): Promise<Dispute[]> {
    return this.disputes.find({ where: { userId }, order: { createdAt: 'DESC' } });
  }

  async listAllDisputes(): Promise<Dispute[]> {
    return this.disputes.find({ order: { createdAt: 'DESC' } });
  }

  async updateDispute(disputeId: string, adminId: string, dto: UpdateDisputeDto): Promise<Dispute> {
    const dispute = await this.disputes.findOneBy({ id: disputeId });
    if (!dispute) throw new NotFoundException('Dispute not found');

    if (dto.status !== undefined) dispute.status = dto.status;
    if (dto.internalNotes !== undefined) dispute.internalNotes = dto.internalNotes;
    if (dto.assignedTo !== undefined) dispute.assignedTo = dto.assignedTo;
    await this.disputes.save(dispute);

    await this.auditService.record({
      action: 'compliance.dispute_updated',
      actorId: adminId,
      actorType: 'admin',
      targetId: dispute.id,
      metadata: { status: dispute.status },
    });

    return dispute;
  }

  async createSar(adminId: string, dto: CreateSarDto): Promise<SuspiciousActivityReport> {
    const sar = await this.sars.save(
      this.sars.create({
        subjectUserId: dto.subjectUserId,
        relatedOperationIds: dto.relatedOperationIds,
        reason: dto.reason,
        filedBy: adminId,
      }),
    );

    await this.auditService.record({
      action: 'compliance.sar_documented',
      actorId: adminId,
      actorType: 'admin',
      targetId: sar.id,
      metadata: { subjectUserId: dto.subjectUserId },
    });

    return sar;
  }

  async listSars(): Promise<SuspiciousActivityReport[]> {
    return this.sars.find({ order: { createdAt: 'DESC' } });
  }
}
