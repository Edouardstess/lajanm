import { IsInt, IsOptional, IsString, IsUUID, Min } from 'class-validator';

export class InitiatePayoutDto {
  @IsInt()
  @Min(1)
  amountHTG: number;

  /** Same purpose as wallet transfer's clientRequestId — see TransferDto. */
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
