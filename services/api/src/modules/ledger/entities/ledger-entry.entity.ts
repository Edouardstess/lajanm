import { Column, CreateDateColumn, Entity, Index, JoinColumn, ManyToOne, PrimaryGeneratedColumn } from 'typeorm';
import { Account } from './account.entity';
import { Operation } from './operation.entity';

export enum EntryDirection {
  DEBIT = 'debit',
  CREDIT = 'credit',
}

/**
 * One leg of a double-entry movement. A LedgerEntry row is never updated or
 * deleted once written (enforced at the DB level, see the
 * AddLedgerImmutability migration) — correcting a mistake means posting a
 * new Operation with reversing entries, so the full history is always
 * reconstructable and auditable.
 *
 * amountMinor is stored in the currency's smallest unit (centimes for HTG)
 * as a bigint-backed integer to avoid floating point drift on money.
 */
@Entity('ledger_entries')
@Index(['accountId', 'createdAt'])
export class LedgerEntry {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  operationId: string;

  @ManyToOne(() => Operation, { onDelete: 'RESTRICT' })
  @JoinColumn({ name: 'operationId' })
  operation: Operation;

  @Index()
  @Column({ type: 'uuid' })
  accountId: string;

  @ManyToOne(() => Account, { onDelete: 'RESTRICT' })
  @JoinColumn({ name: 'accountId' })
  account: Account;

  @Column({ type: 'enum', enum: EntryDirection })
  direction: EntryDirection;

  // Always positive; sign/effect on balance comes from `direction`.
  @Column({ type: 'bigint' })
  amountMinor: string;

  @Column({ type: 'varchar', length: 3, default: 'HTG' })
  currency: string;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
