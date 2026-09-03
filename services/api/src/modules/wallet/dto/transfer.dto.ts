import { IsInt, IsUUID, Matches, Min } from 'class-validator';

export class TransferDto {
  @Matches(/^\+?[0-9]{8,15}$/, { message: 'recipientPhone must be a valid phone number' })
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
}
