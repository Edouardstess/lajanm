import { BadRequestException, Injectable, NotFoundException } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { User, UserTier } from '../auth/entities/user.entity';
import { DecideKycDto, KycDecision } from './dto/decide-kyc.dto';
import { SubmitKycDto } from './dto/submit-kyc.dto';
import { KycStatus, KycSubmission } from './entities/kyc-submission.entity';

@Injectable()
export class KycService {
  constructor(
    @InjectRepository(KycSubmission) private readonly submissions: Repository<KycSubmission>,
    @InjectRepository(User) private readonly users: Repository<User>,
    private readonly auditService: AuditService,
  ) {}

  async submit(userId: string, dto: SubmitKycDto): Promise<KycSubmission> {
    const pending = await this.submissions.findOneBy({ userId, status: KycStatus.PENDING });
    if (pending) {
      throw new BadRequestException('A KYC submission is already pending review');
    }

    const submission = await this.submissions.save(
      this.submissions.create({
        userId,
        idDocumentUrl: dto.idDocumentUrl,
        selfieUrl: dto.selfieUrl,
        status: KycStatus.PENDING,
      }),
    );

    await this.auditService.record({
      action: 'kyc.submitted',
      actorId: userId,
      actorType: 'user',
      targetId: submission.id,
    });

    return submission;
  }

  async findMine(userId: string): Promise<KycSubmission[]> {
    return this.submissions.find({ where: { userId }, order: { createdAt: 'DESC' } });
  }

  /**
   * The pending-review queue. No role check yet — the back-office module
   * (compliance/admin auth) adds operator/admin RBAC around this endpoint;
   * for now it only requires a valid user session.
   */
  async listQueue(): Promise<KycSubmission[]> {
    return this.submissions.find({
      where: { status: KycStatus.PENDING },
      order: { createdAt: 'ASC' },
    });
  }

  async decide(
    submissionId: string,
    reviewerId: string,
    dto: DecideKycDto,
  ): Promise<KycSubmission> {
    const submission = await this.submissions.findOneBy({ id: submissionId });
    if (!submission) {
      throw new NotFoundException('KYC submission not found');
    }
    if (submission.status !== KycStatus.PENDING) {
      throw new BadRequestException('This submission has already been reviewed');
    }

    submission.status =
      dto.decision === KycDecision.APPROVED ? KycStatus.APPROVED : KycStatus.REJECTED;
    submission.reviewerId = reviewerId;
    submission.reviewedAt = new Date();
    submission.rejectionReason = dto.rejectionReason ?? null;
    await this.submissions.save(submission);

    if (submission.status === KycStatus.APPROVED) {
      await this.users.update({ id: submission.userId }, { tier: UserTier.VERIFIED });
    }

    await this.auditService.record({
      action: 'kyc.decision',
      actorId: reviewerId,
      actorType: 'admin',
      targetId: submission.id,
      metadata: { decision: submission.status, userId: submission.userId },
    });

    return submission;
  }
}
