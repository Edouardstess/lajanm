import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { AuditLog } from './entities/audit-log.entity';

export interface RecordAuditEventInput {
  action: string;
  actorId: string;
  actorType: 'user' | 'admin' | 'system';
  targetId?: string;
  metadata?: Record<string, unknown>;
  ipAddress?: string;
}

/**
 * The only supported way to write to audit_logs. There is deliberately no
 * update() or delete() here — see AuditLog entity doc for why.
 */
@Injectable()
export class AuditService {
  constructor(
    @InjectRepository(AuditLog) private readonly auditLogRepository: Repository<AuditLog>,
  ) {}

  async record(event: RecordAuditEventInput): Promise<AuditLog> {
    const entry = this.auditLogRepository.create({
      action: event.action,
      actorId: event.actorId,
      actorType: event.actorType,
      targetId: event.targetId ?? null,
      metadata: event.metadata ?? null,
      ipAddress: event.ipAddress ?? null,
    });
    return this.auditLogRepository.save(entry);
  }

  async findByActor(actorId: string): Promise<AuditLog[]> {
    return this.auditLogRepository.find({
      where: { actorId },
      order: { createdAt: 'DESC' },
    });
  }

  async findByTarget(targetId: string): Promise<AuditLog[]> {
    return this.auditLogRepository.find({
      where: { targetId },
      order: { createdAt: 'DESC' },
    });
  }
}
