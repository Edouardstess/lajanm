import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn } from 'typeorm';

export enum OtpPurpose {
  TRANSFER = 'transfer',
  PAYOUT = 'payout',
}

/**
 * A one-time code required before a sensitive transaction at/above
 * OTP_THRESHOLD_HTG completes (see SecurityService). The code itself is
 * never stored — only an HMAC of it, so a database read can't leak a
 * usable code. maxAttempts bounds brute-force guessing of the (low
 * entropy, 6-digit) code within the row's lifetime.
 */
@Entity('otp_codes')
export class OtpCode {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  userId: string;

  @Column({ type: 'enum', enum: OtpPurpose })
  purpose: OtpPurpose;

  @Column({ type: 'varchar', length: 64 })
  codeHash: string;

  @Column({ type: 'int', default: 0 })
  attempts: number;

  @Column({ type: 'int', default: 3 })
  maxAttempts: number;

  @Column({ type: 'timestamptz' })
  expiresAt: Date;

  @Column({ type: 'timestamptz', nullable: true })
  consumedAt: Date | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
