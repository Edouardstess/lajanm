import { IsEnum, IsOptional, IsUUID } from 'class-validator';
import { TicketStatus } from '../entities/support-ticket.entity';

export class UpdateTicketDto {
  @IsOptional()
  @IsEnum(TicketStatus)
  status?: TicketStatus;

  @IsOptional()
  @IsUUID()
  assignedTo?: string;
}
