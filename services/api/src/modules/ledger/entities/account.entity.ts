import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn } from 'typeorm';

export enum AccountOwnerType {
  USER = 'user',
  SYSTEM = 'system',
}

/**
 * A ledger account. Every user wallet is one account; external rails
 * (MonCash float, NatCash float, fee income, etc.) are modeled as system
 * accounts so every movement of money — including money entering or
 * leaving Lajan'm — is a balanced double entry, never a bare credit.
 *
 * This table never stores a mutable balance. Balance is always derived
 * from the sum of ledger_entries for the account (see LedgerService).
 */
@Entity('accounts')
export class Account {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Column({ type: 'enum', enum: AccountOwnerType })
  ownerType: AccountOwnerType;

  // For USER accounts, the owning user's id. Null for SYSTEM accounts.
  @Index()
  @Column({ type: 'uuid', nullable: true })
  ownerId: string | null;

  // e.g. 'wallet', 'moncash_float', 'natcash_float', 'fees'
  @Column({ type: 'varchar', length: 64 })
  name: string;

  @Column({ type: 'varchar', length: 3, default: 'HTG' })
  currency: string;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
