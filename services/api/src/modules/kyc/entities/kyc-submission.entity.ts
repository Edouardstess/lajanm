import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn } from 'typeorm';

export enum KycStatus {
  PENDING = 'pending',
  APPROVED = 'approved',
  REJECTED = 'rejected',
}

/**
 * Une tentative de vérification d'identité.
 *
 * Les deux photos sont désignées par l'identifiant du fichier déposé via
 * POST /uploads/kyc, qui les a inspectées (extension, type annoncé et
 * surtout octets réels) avant de les écrire. Ce module possède le
 * circuit de revue et son statut, pas le stockage.
 */
@Entity('kyc_submissions')
export class KycSubmission {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  userId: string;

  /**
   * Références vers `uploaded_files`. Nullables uniquement à cause des
   * demandes créées avant l'existence d'un vrai dépôt de fichiers ; toute
   * nouvelle demande les renseigne (voir SubmitKycDto).
   */
  @Column({ type: 'uuid', nullable: true })
  idDocumentFileId: string | null;

  @Column({ type: 'uuid', nullable: true })
  selfieFileId: string | null;

  @Column({ type: 'enum', enum: KycStatus, default: KycStatus.PENDING })
  status: KycStatus;

  @Column({ type: 'uuid', nullable: true })
  reviewerId: string | null;

  @Column({ type: 'timestamptz', nullable: true })
  reviewedAt: Date | null;

  @Column({ type: 'varchar', length: 255, nullable: true })
  rejectionReason: string | null;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
