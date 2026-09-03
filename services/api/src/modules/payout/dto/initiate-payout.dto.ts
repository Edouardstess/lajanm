import { IsInt, IsUUID, Min } from 'class-validator';

export class InitiatePayoutDto {
  @IsInt()
  @Min(1)
  amountHTG: number;

  /** Same purpose as wallet transfer's clientRequestId — see TransferDto. */
  @IsUUID()
  clientRequestId: string;
}
