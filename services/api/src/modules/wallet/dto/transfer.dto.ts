import { IsInt, IsOptional, IsString, IsUUID, Matches, Min } from 'class-validator';

export class TransferDto {
  /**
   * Volontairement permissif : les séparateurs sont admis ici, et c'est
   * `normalizePhone` (common/phone.ts) qui tranche ensuite. L'expression
   * stricte d'avant refusait « +509 34 11 00 02 » AVANT que la
   * normalisation ne s'exécute — le numéro était donc valide pour le
   * service et rejeté par le DTO, et son message d'erreur générique
   * n'expliquait pas ce qui n'allait pas.
   */
  @Matches(/^[+0-9][0-9 .()-]{6,30}$/, {
    message: 'recipientPhone must be a phone number (digits, optional + and separators)',
  })
  recipientPhone: string;

  @IsInt()
  @Min(1)
  amountHTG: number;

  /**
   * Client-generated UUID, one per transfer attempt — this is what makes
   * a retried tap-to-send (flaky network, double-tap) safe: it becomes the
   * ledger idempotency key, so the same clientRequestId can never move
   * money twice (see LedgerService.postOperation).
   */
  @IsUUID()
  clientRequestId: string;

  /**
   * Required only when amountHTG is at/above SecurityService's OTP
   * threshold — obtained from POST /security/otp/request beforehand.
   */
  @IsOptional()
  @IsUUID()
  otpRequestId?: string;

  @IsOptional()
  @IsString()
  otpCode?: string;
}
