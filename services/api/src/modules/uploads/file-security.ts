import { BadRequestException } from '@nestjs/common';

/**
 * Contrôle de sûreté des fichiers déposés par les clients.
 *
 * Le principe, et il n'a qu'une phrase : **on ne fait confiance ni au nom
 * du fichier, ni à son extension, ni au Content-Type annoncé.** Ces trois
 * valeurs sont fournies par l'appelant, donc choisies par un attaquant.
 * Seul le contenu réel du fichier décide.
 *
 * Un fichier n'est accepté que si les quatre conditions tiennent :
 *   1. son extension figure dans la liste blanche ;
 *   2. son Content-Type annoncé figure dans la liste blanche ;
 *   3. ses premiers octets correspondent à un format d'image attendu, et
 *      ce format est celui que l'extension annonçait ;
 *   4. sa taille est dans les bornes.
 *
 * La condition 3 est la seule qui compte vraiment : un `.jpg` dont les
 * octets disent « HTML » est un fichier HTML, quoi qu'en dise son nom.
 */

/** 8 Mio : une photo de pièce d'identité au téléphone en fait 2 à 4. */
export const MAX_FILE_BYTES = 8 * 1024 * 1024;

/**
 * Plancher : en dessous, aucune image n'est exploitable pour une
 * vérification d'identité, et c'est le poids d'une charge utile déguisée.
 */
export const MIN_FILE_BYTES = 1024;

export type AllowedFormat = 'jpeg' | 'png' | 'webp';

interface FormatSpec {
  format: AllowedFormat;
  extensions: readonly string[];
  mimeTypes: readonly string[];
  /** Renvoie vrai si les octets de tête correspondent au format. */
  matches: (buffer: Buffer) => boolean;
}

/**
 * Les signatures, telles que définies par chaque format :
 *   JPEG  FF D8 FF                        (SOI + marqueur)
 *   PNG   89 50 4E 47 0D 0A 1A 0A         (signature à 8 octets)
 *   WebP  'RIFF' …taille… 'WEBP'          (conteneur RIFF)
 *
 * Le GIF est volontairement absent : animé, il n'apporte rien à une
 * vérification d'identité et élargit la surface d'attaque des décodeurs.
 * Le SVG aussi, et pour une raison plus grave : c'est du XML qui peut
 * porter du script.
 */
const FORMATS: readonly FormatSpec[] = [
  {
    format: 'jpeg',
    extensions: ['.jpg', '.jpeg'],
    mimeTypes: ['image/jpeg'],
    matches: (b) => b.length >= 3 && b[0] === 0xff && b[1] === 0xd8 && b[2] === 0xff,
  },
  {
    format: 'png',
    extensions: ['.png'],
    mimeTypes: ['image/png'],
    matches: (b) =>
      b.length >= 8 &&
      b.subarray(0, 8).equals(Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])),
  },
  {
    format: 'webp',
    extensions: ['.webp'],
    mimeTypes: ['image/webp'],
    matches: (b) =>
      b.length >= 12 &&
      b.subarray(0, 4).toString('ascii') === 'RIFF' &&
      b.subarray(8, 12).toString('ascii') === 'WEBP',
  },
];

export const ALLOWED_EXTENSIONS: readonly string[] = FORMATS.flatMap((f) => f.extensions);
export const ALLOWED_MIME_TYPES: readonly string[] = FORMATS.flatMap((f) => f.mimeTypes);

/**
 * Motifs qui n'ont rien à faire au début d'une image et qui trahissent un
 * fichier polyglotte — un fichier valide pour deux formats à la fois,
 * servi comme image ici et exécuté comme script ailleurs.
 */
const HOSTILE_HEADS: ReadonlyArray<{ label: string; bytes: Buffer }> = [
  { label: 'HTML', bytes: Buffer.from('<!DOCTYPE', 'ascii') },
  { label: 'HTML', bytes: Buffer.from('<html', 'ascii') },
  { label: 'a script', bytes: Buffer.from('<script', 'ascii') },
  { label: 'SVG', bytes: Buffer.from('<svg', 'ascii') },
  { label: 'XML', bytes: Buffer.from('<?xml', 'ascii') },
  { label: 'PHP', bytes: Buffer.from('<?php', 'ascii') },
  { label: 'a shell script', bytes: Buffer.from('#!', 'ascii') },
  { label: 'a ZIP archive', bytes: Buffer.from([0x50, 0x4b, 0x03, 0x04]) },
  { label: 'an ELF executable', bytes: Buffer.from([0x7f, 0x45, 0x4c, 0x46]) },
  { label: 'a Windows executable', bytes: Buffer.from('MZ', 'ascii') },
];

export interface InspectedFile {
  format: AllowedFormat;
  extension: string;
  sizeBytes: number;
}

/**
 * Inspecte un fichier reçu et lève si quoi que ce soit cloche.
 *
 * @param originalName nom annoncé par le client — utilisé UNIQUEMENT pour
 *                     lire l'extension, jamais pour nommer le fichier
 *                     stocké (voir `uploads.service.ts`).
 */
export function inspectUpload(
  originalName: string,
  declaredMimeType: string,
  buffer: Buffer,
): InspectedFile {
  if (buffer.length > MAX_FILE_BYTES) {
    throw new BadRequestException(
      `File is larger than ${Math.round(MAX_FILE_BYTES / 1024 / 1024)} MB`,
    );
  }
  if (buffer.length < MIN_FILE_BYTES) {
    throw new BadRequestException('File is too small to be a usable photo');
  }

  const extension = extractExtension(originalName);
  if (!ALLOWED_EXTENSIONS.includes(extension)) {
    throw new BadRequestException(
      `Only ${ALLOWED_EXTENSIONS.join(', ')} files are accepted (received "${extension || 'no extension'}")`,
    );
  }

  const declared = declaredMimeType.split(';')[0].trim().toLowerCase();
  if (!ALLOWED_MIME_TYPES.includes(declared)) {
    throw new BadRequestException(`Content type "${declared}" is not accepted`);
  }

  for (const hostile of HOSTILE_HEADS) {
    if (buffer.subarray(0, hostile.bytes.length).equals(hostile.bytes)) {
      throw new BadRequestException(`File content looks like ${hostile.label}, not an image`);
    }
  }

  const actual = FORMATS.find((f) => f.matches(buffer));
  if (!actual) {
    throw new BadRequestException('File content is not a JPEG, PNG or WebP image');
  }

  // Le point clé : l'extension doit dire la vérité sur le contenu. Un
  // « .png » dont les octets sont du JPEG est refusé — non parce que le
  // JPEG serait dangereux, mais parce qu'un fichier qui ment sur sa
  // nature est exactement ce qu'on cherche à écarter.
  if (!actual.extensions.includes(extension)) {
    throw new BadRequestException(
      `File content is ${actual.format.toUpperCase()} but the name says "${extension}"`,
    );
  }
  if (!actual.mimeTypes.includes(declared)) {
    throw new BadRequestException(
      `File content is ${actual.format.toUpperCase()} but the content type says "${declared}"`,
    );
  }

  return { format: actual.format, extension, sizeBytes: buffer.length };
}

/**
 * Extension en minuscules, dernier point seulement.
 *
 * Le nom est d'abord réduit à son dernier segment : un client qui envoie
 * « ../../etc/passwd.jpg » ne doit pas pouvoir peser sur ce qui est lu.
 * (Le nom ne sert de toute façon jamais à écrire sur le disque.)
 */
function extractExtension(originalName: string): string {
  const base = (originalName ?? '').split(/[\\/]/).pop() ?? '';
  const dot = base.lastIndexOf('.');
  if (dot <= 0 || dot === base.length - 1) return '';
  return base.slice(dot).toLowerCase();
}
