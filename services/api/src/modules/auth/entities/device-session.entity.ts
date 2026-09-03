import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn } from 'typeorm';

/**
 * One row per (user, device) pair a user has logged in from. Lets the
 * profile screen list "connected devices" and revoke one — revoking sets
 * revokedAt rather than deleting the row, preserving history for audit.
 */
@Entity('device_sessions')
@Index(['userId', 'deviceId'], { unique: true })
export class DeviceSession {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  userId: string;

  // Client-generated stable identifier for the physical device/install.
  @Column({ type: 'varchar', length: 255 })
  deviceId: string;

  @Column({ type: 'varchar', length: 128, nullable: true })
  deviceName: string | null;

  @Column({ type: 'timestamptz' })
  lastSeenAt: Date;

  @Column({ type: 'timestamptz', nullable: true })
  revokedAt: Date | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
