import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn } from 'typeorm';

export enum FraudFlagStatus {
  OPEN = 'open',
  RESOLVED = 'resolved',
  CONFIRMED_SUSPECT = 'confirmed_suspect',
  FALSE_POSITIVE = 'false_positive',
}

export enum FraudRuleCode {
  HIGH_VELOCITY = 'high_velocity',
  AMOUNT_ANOMALY = 'amount_anomaly',
  FREQUENT_NEW_BENEFICIARIES = 'frequent_new_beneficiaries',
}

/**
 * A velocity-rule hit — flagged for a human to review (compliance
 * module), never auto-blocking. See FraudService.evaluate.
 */
@Entity('fraud_flags')
export class FraudFlag {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  userId: string;

  @Column({ type: 'enum', enum: FraudRuleCode })
  ruleCode: FraudRuleCode;

  @Column({ type: 'enum', enum: FraudFlagStatus, default: FraudFlagStatus.OPEN })
  status: FraudFlagStatus;

  @Column({ type: 'uuid', nullable: true })
  relatedOperationId: string | null;

  @Column({ type: 'jsonb', nullable: true })
  details: Record<string, unknown> | null;

  @Column({ type: 'uuid', nullable: true })
  resolvedBy: string | null;

  @Column({ type: 'timestamptz', nullable: true })
  resolvedAt: Date | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
