import { IsUUID } from 'class-validator';

/**
 * Les deux pièces sont désignées par l'identifiant renvoyé par
 * POST /uploads/kyc.
 *
 * La version précédente acceptait n'importe quelle chaîne non vide, et
 * l'application y mettait l'URI local du téléphone (« file:///… ») — une
 * valeur qui ne désigne rien côté serveur. Aucun fichier n'était donc
 * réellement transmis, et rien n'était contrôlé. Exiger un UUID force le
 * passage par l'endpoint qui, lui, inspecte le contenu.
 */
export class SubmitKycDto {
  @IsUUID()
  idDocumentFileId: string;

  @IsUUID()
  selfieFileId: string;
}
