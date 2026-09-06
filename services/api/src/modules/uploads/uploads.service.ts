import { BadRequestException, Injectable, Logger, NotFoundException } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { InjectRepository } from '@nestjs/typeorm';
import { createHash, randomUUID } from 'crypto';
import { promises as fs } from 'fs';
import { join, resolve } from 'path';
import { In, Repository } from 'typeorm';
import { RateLimitService } from '../../common/rate-limit.service';
import { AuditService } from '../audit/audit.service';
import { UploadedFile } from './entities/uploaded-file.entity';
import { inspectUpload, MAX_FILE_BYTES } from './file-security';

// Un utilisateur légitime dépose deux photos, éventuellement reprises
// quelques fois. 20 par heure laisse la place aux reprises et empêche de
// remplir le disque.
const UPLOAD_LIMIT = 20;
const UPLOAD_WINDOW_SECONDS = 3600;

export interface AcceptedUpload {
  fileId: string;
  format: string;
  sizeBytes: number;
}

@Injectable()
export class UploadsService {
  private readonly logger = new Logger(UploadsService.name);
  private readonly uploadDir: string;

  constructor(
    @InjectRepository(UploadedFile) private readonly files: Repository<UploadedFile>,
    private readonly config: ConfigService,
    private readonly rateLimitService: RateLimitService,
    private readonly auditService: AuditService,
  ) {
    this.uploadDir = resolve(this.config.get<string>('UPLOAD_DIR') ?? './var/uploads');
  }

  /**
   * Accepte un fichier, ou explique pourquoi il est refusé.
   *
   * L'ordre compte : on limite, on inspecte, ET SEULEMENT ENSUITE on
   * écrit. Rien de refusé ne touche jamais le disque.
   */
  async accept(
    ownerId: string,
    file: { originalname: string; mimetype: string; buffer: Buffer } | undefined,
  ): Promise<AcceptedUpload> {
    if (!file?.buffer) {
      throw new BadRequestException('No file received');
    }

    await this.rateLimitService.consume(`uploads:${ownerId}`, UPLOAD_LIMIT, UPLOAD_WINDOW_SECONDS);

    const inspected = inspectUpload(file.originalname, file.mimetype, file.buffer);

    const sha256 = createHash('sha256').update(file.buffer).digest('hex');
    // Le nom sur le disque est fabriqué ici, à partir d'un UUID et de
    // l'extension VALIDÉE. Aucun octet fourni par le client n'entre dans
    // un chemin de fichier : c'est ce qui rend la traversée de répertoire
    // structurellement impossible, plutôt que filtrée.
    const storedName = `${randomUUID()}${inspected.extension}`;

    await fs.mkdir(this.uploadDir, { recursive: true, mode: 0o700 });
    // 0600 : lisible par le seul compte qui fait tourner l'API. Ce sont
    // des pièces d'identité.
    await fs.writeFile(join(this.uploadDir, storedName), file.buffer, { mode: 0o600 });

    const saved = await this.files.save(
      this.files.create({
        ownerId,
        format: inspected.format,
        sizeBytes: inspected.sizeBytes,
        sha256,
        storedName,
      }),
    );

    await this.auditService.record({
      action: 'upload.accepted',
      actorId: ownerId,
      actorType: 'user',
      targetId: saved.id,
      metadata: { format: inspected.format, sizeBytes: inspected.sizeBytes, sha256 },
    });

    return { fileId: saved.id, format: saved.format, sizeBytes: saved.sizeBytes };
  }

  /**
   * Vérifie que ces identifiants existent ET appartiennent à l'appelant.
   *
   * Sans ce contrôle, il suffirait de deviner l'identifiant d'un fichier
   * pour joindre la pièce d'identité de quelqu'un d'autre à sa propre
   * demande de vérification.
   */
  async assertOwnedBy(ownerId: string, fileIds: string[]): Promise<UploadedFile[]> {
    const unique = [...new Set(fileIds)];
    const found = await this.files.find({ where: { id: In(unique), ownerId } });

    if (found.length !== unique.length) {
      // Un 404 plutôt qu'un 403 : distinguer « n'existe pas » de « pas à
      // vous » indiquerait quels identifiants sont valides.
      throw new NotFoundException('Unknown file reference');
    }
    return found;
  }

  /** Lecture du contenu, pour le back-office qui instruit un dossier. */
  async read(fileId: string): Promise<{ file: UploadedFile; content: Buffer }> {
    const file = await this.files.findOneBy({ id: fileId });
    if (!file) throw new NotFoundException('Unknown file reference');

    const path = join(this.uploadDir, file.storedName);
    let content: Buffer;
    try {
      content = await fs.readFile(path);
    } catch {
      // Le disque de l'hébergement peut être éphémère : le dire
      // franchement plutôt que renvoyer une image vide.
      this.logger.error(`Fichier ${file.id} absent du disque (${path})`);
      throw new NotFoundException('File content is no longer available');
    }

    // Le contenu relu doit être celui qui a été accepté. Une divergence
    // signifie qu'il a été remplacé sur le disque après coup.
    const actual = createHash('sha256').update(content).digest('hex');
    if (actual !== file.sha256) {
      this.logger.error(`Empreinte différente pour ${file.id} : contenu modifié sur le disque`);
      throw new NotFoundException('File content failed its integrity check');
    }

    return { file, content };
  }

  static get maxBytes(): number {
    return MAX_FILE_BYTES;
  }
}
