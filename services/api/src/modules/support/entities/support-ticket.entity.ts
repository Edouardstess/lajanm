import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn, UpdateDateColumn } from 'typeorm';

export enum TicketCategory {
  GENERAL = 'general',
  TRANSACTION = 'transaction',
  KYC = 'kyc',
  TECHNICAL = 'technical',
  OTHER = 'other',
}

export enum TicketStatus {
  OPEN = 'open',
  IN_PROGRESS = 'in_progress',
  RESOLVED = 'resolved',
  CLOSED = 'closed',
}

/** A customer support ticket. The conversation itself lives in SupportMessage. */
@Entity('support_tickets')
export class SupportTicket {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  userId: string;

  @Column({ type: 'varchar', length: 255 })
  subject: string;

  @Column({ type: 'enum', enum: TicketCategory, default: TicketCategory.GENERAL })
  category: TicketCategory;

  @Column({ type: 'enum', enum: TicketStatus, default: TicketStatus.OPEN })
  status: TicketStatus;

  @Column({ type: 'uuid', nullable: true })
  assignedTo: string | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;

  @UpdateDateColumn({ type: 'timestamptz' })
  updatedAt: Date;
}
