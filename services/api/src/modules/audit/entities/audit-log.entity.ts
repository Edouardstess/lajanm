import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn } from 'typeorm';

/**
 * Append-only audit trail (NF-06). Every sensitive action across every
 * module — login, PIN change, KYC decision, transaction, admin action —
 * is written here and never updated or deleted afterwards. Immutability is
 * enforced twice: at the application layer (AuditService exposes only
 * create/find, no update/delete) and at the database layer (a trigger
 * rejects UPDATE/DELETE on this table — see AddAuditLogImmutability
 * migration), so a compromised application server still cannot rewrite
 * history without also compromising the database itself.
 */
@Entity('audit_logs')
@Index(['actorId', 'createdAt'])
export class AuditLog {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  // e.g. 'auth.login', 'kyc.decision', 'ledger.operation.posted', 'admin.dispute.resolved'
  @Index()
  @Column({ type: 'varchar', length: 128 })
  action: string;

  // Who performed the action: a user id, an admin id, or 'system' for
  // automated processes (webhooks, scheduled jobs).
  @Column({ type: 'varchar', length: 128 })
  actorId: string;

  @Column({ type: 'varchar', length: 32 })
  actorType: 'user' | 'admin' | 'system';

  // The entity the action was performed on, when applicable (e.g. an
  // operationId, a userId being reviewed).
  @Column({ type: 'varchar', length: 128, nullable: true })
  targetId: string | null;

  @Column({ type: 'jsonb', nullable: true })
  metadata: Record<string, unknown> | null;

  @Column({ type: 'varchar', length: 64, nullable: true })
  ipAddress: string | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
