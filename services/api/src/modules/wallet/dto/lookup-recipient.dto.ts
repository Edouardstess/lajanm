import { IsString, MaxLength, MinLength } from 'class-validator';

export class LookupRecipientDto {
  /**
   * Volontairement peu contraint ici : c'est `normalizePhone` qui juge,
   * et son message d'erreur explique le format attendu. Un `@Matches`
   * dupliqué renverrait « invalide » sans dire pourquoi.
   */
  @IsString()
  @MinLength(1)
  @MaxLength(32)
  phone: string;
}
