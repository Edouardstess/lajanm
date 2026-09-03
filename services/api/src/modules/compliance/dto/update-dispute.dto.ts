import { IsEnum, IsOptional, IsString, IsUUID } from 'class-validator';
import { DisputeStatus } from '../entities/dispute.entity';

export class UpdateDisputeDto {
  @IsOptional()
  @IsEnum(DisputeStatus)
  status?: DisputeStatus;

  @IsOptional()
  @IsString()
  internalNotes?: string;

  @IsOptional()
  @IsUUID()
  assignedTo?: string;
}
