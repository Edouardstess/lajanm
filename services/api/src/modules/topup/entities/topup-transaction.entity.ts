import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn, UpdateDateColumn } from 'typeorm';

export enum TopupSource {
  MONCASH = 'moncash',
  // Not implemented yet (Phase 2 of the product plan) — the column exists
  // now so history/reporting never needs a migration to add it later.
  NATCASH = 'natcash',
}

export enum TopupStatus {
  // MonCash payment created, waiting on the webhook confirmation.
  PENDING = 'pending',
  COMPLETED = 'completed',
  FAILED = 'failed',
}

/**
 * One user-initiated top-up attempt. The ledger is only ever credited from
 * the webhook handler (see TopupService.handleWebhook) — creating this row
 * does NOT move any money, it just records that a payment was requested so
 * the mobile app has something to poll and the webhook has something to
 * match against.
 */
@Entity('topup_transactions')
export class TopupTransaction {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  userId: string;

  @Column({ type: 'enum', enum: TopupSource, default: TopupSource.MONCASH })
  source: TopupSource;

  @Column({ type: 'bigint' })
  amountMinor: string;

  @Column({ type: 'varchar', length: 3, default: 'HTG' })
  currency: string;

  @Column({ type: 'enum', enum: TopupStatus, default: TopupStatus.PENDING })
  status: TopupStatus;

  // MonCash's own transaction reference — the idempotency key for the
  // ledger credit. Null until MonCash has assigned one (e.g. while a
  // retry is still queued).
  @Index({ unique: true, where: '"providerReference" IS NOT NULL' })
  @Column({ type: 'varchar', length: 128, nullable: true })
  providerReference: string | null;

  // Set once the webhook has credited the ledger.
  @Column({ type: 'uuid', nullable: true })
  operationId: string | null;

  @Column({ type: 'varchar', length: 255, nullable: true })
  failureReason: string | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;

  @UpdateDateColumn({ type: 'timestamptz' })
  updatedAt: Date;
}
