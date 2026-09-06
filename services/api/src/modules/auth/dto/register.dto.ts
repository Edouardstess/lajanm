import { IsString, Matches } from 'class-validator';

export class RegisterDto {
  @IsString()
  /**
   * Volontairement permissif : les séparateurs sont admis ici, et c'est
   * `normalizePhone` (common/phone.ts) qui tranche ensuite. L'expression
   * stricte d'avant refusait « +509 34 11 00 02 » AVANT que la
   * normalisation ne s'exécute — le numéro était donc valide pour le
   * service et rejeté par le DTO, et son message d'erreur générique
   * n'expliquait pas ce qui n'allait pas.
   */
  @Matches(/^[+0-9][0-9 .()-]{6,30}$/, {
    message: 'phone must be a phone number (digits, optional + and separators)',
  })
  phone: string;

  @IsString()
  @Matches(/^[0-9]{4,6}$/, { message: 'pin must be 4 to 6 digits' })
  pin: string;
}
