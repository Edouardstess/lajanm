import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn, UpdateDateColumn } from 'typeorm';

export enum PayoutStatus {
  COMPLETED = 'completed',
  // Funds were debited and reversed — the user's balance is made whole,
  // this row exists only as a record of the attempt.
  FAILED = 'failed',
}

/**
 * One payout (withdrawal to MonCash) attempt. Unlike top-up, MonCash's
 * cash-out call is modeled as synchronous — see MonCashClient.createPayout
 * — so by the time PayoutService.initiate() returns, the outcome is
 * already final: this row is never left "pending" the way a
 * TopupTransaction can be.
 */
@Entity('payout_transactions')
export class PayoutTransaction {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  userId: string;

  @Column({ type: 'bigint' })
  amountMinor: string;

  @Column({ type: 'varchar', length: 3, default: 'HTG' })
  currency: string;

  @Column({ type: 'enum', enum: PayoutStatus })
  status: PayoutStatus;

  // The reserving debit operation (always created, even on failure).
  @Column({ type: 'uuid' })
  operationId: string;

  // Set only when the reserve had to be reversed.
  @Column({ type: 'uuid', nullable: true })
  reversalOperationId: string | null;

  @Column({ type: 'varchar', length: 128, nullable: true })
  providerReference: string | null;

  @Column({ type: 'varchar', length: 255, nullable: true })
  failureReason: string | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;

  @UpdateDateColumn({ type: 'timestamptz' })
  updatedAt: Date;
}
