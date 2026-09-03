import { IsOptional, IsString, IsUUID, MaxLength, MinLength } from 'class-validator';

export class CreateDisputeDto {
  @IsString()
  @MaxLength(255)
  subject: string;

  @IsString()
  @MinLength(1)
  description: string;

  @IsOptional()
  @IsUUID()
  relatedOperationId?: string;
}
