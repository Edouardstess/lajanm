import { IsEnum, IsOptional, IsString, MaxLength, ValidateIf } from 'class-validator';

export enum KycDecision {
  APPROVED = 'approved',
  REJECTED = 'rejected',
}

export class DecideKycDto {
  @IsEnum(KycDecision)
  decision: KycDecision;

  @ValidateIf((dto: DecideKycDto) => dto.decision === KycDecision.REJECTED)
  @IsString()
  @MaxLength(255)
  @IsOptional()
  rejectionReason?: string;
}
