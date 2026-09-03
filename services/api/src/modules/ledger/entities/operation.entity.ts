import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn, UpdateDateColumn } from 'typeorm';

export enum OperationType {
  TOPUP = 'topup',
  PAYOUT = 'payout',
  TRANSFER = 'transfer',
  ADJUSTMENT = 'adjustment',
}

export enum OperationStatus {
  // Funds are reserved/held but not yet finally confirmed by an external rail.
  PENDING = 'pending',
  COMPLETED = 'completed',
  // A pending operation was reversed with correcting entries (see NF requirement:
  // the ledger is append-only — a failed payout is never deleted, only reversed).
  REVERSED = 'reversed',
  FAILED = 'failed',
}

/**
 * One Operation groups the balanced set of LedgerEntry rows that make up a
 * single business event (a top-up, a transfer, a payout...). The
 * idempotencyKey is the mechanism (NF-02) that guarantees a retried request
 * — e.g. a webhook MonCash resends — can never create the same movement twice.
 */
@Entity('operations')
export class Operation {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index({ unique: true })
  @Column({ type: 'varchar', length: 255 })
  idempotencyKey: string;

  @Column({ type: 'enum', enum: OperationType })
  type: OperationType;

  @Column({ type: 'enum', enum: OperationStatus, default: OperationStatus.COMPLETED })
  status: OperationStatus;

  // Free-form context (e.g. { moncashReference, initiatedBy }), never used
  // to store the amount — amounts only ever live in ledger_entries.
  @Column({ type: 'jsonb', nullable: true })
  metadata: Record<string, unknown> | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;

  @UpdateDateColumn({ type: 'timestamptz' })
  updatedAt: Date;
}
