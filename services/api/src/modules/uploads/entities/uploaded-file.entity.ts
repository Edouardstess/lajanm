import { Column, CreateDateColumn, Entity, Index, PrimaryGeneratedColumn } from 'typeorm';

/**
 * Trace d'un fichier accepté : le contenu vit sur le disque, la ligne
 * dit à qui il appartient et ce qu'il est réellement.
 *
 * Le nom d'origine choisi par le client n'est PAS conservé. Il ne sert à
 * rien ici, et tout ce qui le manipulerait plus tard (affichage,
 * téléchargement, écriture) deviendrait une surface d'attaque pour zéro
 * bénéfice.
 */
@Entity('uploaded_files')
export class UploadedFile {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Index()
  @Column({ type: 'uuid' })
  ownerId: string;

  /** Format déduit des octets, jamais de l'extension annoncée. */
  @Column({ type: 'varchar', length: 16 })
  format: string;

  @Column({ type: 'int' })
  sizeBytes: number;

  /**
   * SHA-256 du contenu. Permet de constater qu'un fichier relu est bien
   * celui qui a été accepté, et de repérer un même document redéposé.
   */
  @Column({ type: 'varchar', length: 64 })
  sha256: string;

  /**
   * Nom du fichier sur le disque, généré ici — jamais fourni par le
   * client. Le répertoire n'est pas stocké : il vient de la
   * configuration, donc déplacer le stockage ne réécrit pas la table.
   */
  @Column({ type: 'varchar', length: 128 })
  storedName: string;

  @CreateDateColumn({ type: 'timestamptz' })
  createdAt: Date;
}
