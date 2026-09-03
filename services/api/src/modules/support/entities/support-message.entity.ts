import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn } from 'typeorm';

export enum SupportSenderType {
  USER = 'user',
  ADMIN = 'admin',
}

/** One message in a support ticket's conversation thread. */
@Entity('support_messages')
export class SupportMessage {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  ticketId: string;

  @Column({ type: 'uuid' })
  senderId: string;

  @Column({ type: 'enum', enum: SupportSenderType })
  senderType: SupportSenderType;

  @Column({ type: 'text' })
  body: string;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
