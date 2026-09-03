import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn } from 'typeorm';

/**
 * Internal documentation of a suspicious case, structured to support a
 * future UCREF filing — this table is NOT a transmission mechanism to
 * UCREF (there is no such integration; a real filing is a manual,
 * out-of-band process handled by compliance staff using this record as
 * their working notes). See docs/architecture.md's regulatory notes.
 */
@Entity('suspicious_activity_reports')
export class SuspiciousActivityReport {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  subjectUserId: string;

  // Operation ids relevant to the case — free-form array, not a formal
  // relation, since a report can reference any number of transactions
  // across modules (transfers, payouts, top-ups).
  @Column({ type: 'jsonb' })
  relatedOperationIds: string[];

  @Column({ type: 'text' })
  reason: string;

  @Column({ type: 'uuid' })
  filedBy: string;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
