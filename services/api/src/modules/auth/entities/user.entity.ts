import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn, UpdateDateColumn } from 'typeorm';

export enum UserTier {
  // Lightweight KYC, reduced transaction/limit caps.
  BASIC = 'basic',
  // Full KYC (ID + selfie reviewed and approved), higher caps.
  VERIFIED = 'verified',
}

/**
 * Tier is modeled as an explicit enum column rather than a boolean so
 * additional tiers (e.g. a future "business" tier) can be introduced
 * without a schema migration that touches every caller.
 */
@Entity('users')
export class User {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index({ unique: true })
  @Column({ type: 'varchar', length: 32 })
  phone: string;

  // argon2 hash — the PIN itself is never stored or logged.
  @Column({ type: 'varchar', length: 255 })
  pinHash: string;

  @Column({ type: 'enum', enum: UserTier, default: UserTier.BASIC })
  tier: UserTier;

  @Column({ type: 'varchar', length: 128, nullable: true })
  fullName: string | null;

  @Column({ type: 'varchar', length: 128, nullable: true })
  email: string | null;

  // Failed PIN attempt counter and lock expiry, enforced by the security
  // module (see modules/security) — kept on User because they describe
  // account state, not a security-module-owned record.
  @Column({ type: 'int', default: 0 })
  failedPinAttempts: number;

  @Column({ type: 'timestamptz', nullable: true })
  lockedUntil: Date | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;

  @UpdateDateColumn({ type: 'timestamptz' })
  updatedAt: Date;
}
