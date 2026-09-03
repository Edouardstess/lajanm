import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn } from 'typeorm';

export enum AdminRole {
  OPERATOR = 'operator',
  ADMIN = 'admin',
}

/**
 * A back-office account, entirely separate from `users` (the mobile
 * customer table) — different login endpoint, different JWT guard, no
 * shared identifiers. There is deliberately no self-registration
 * endpoint: the first admin is created via scripts/seed-admin.ts (see
 * docs/architecture.md), and every admin thereafter is created by an
 * existing admin — never exposed over a public route.
 */
@Entity('admin_users')
export class AdminUser {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index({ unique: true })
  @Column({ type: 'varchar', length: 255 })
  email: string;

  @Column({ type: 'varchar', length: 255 })
  passwordHash: string;

  @Column({ type: 'enum', enum: AdminRole, default: AdminRole.OPERATOR })
  role: AdminRole;

  @Column({ type: 'varchar', length: 128, nullable: true })
  fullName: string | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
