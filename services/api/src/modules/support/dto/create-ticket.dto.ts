import { IsEnum, IsOptional, IsString, MaxLength, MinLength } from 'class-validator';
import { TicketCategory } from '../entities/support-ticket.entity';

export class CreateTicketDto {
  @IsString()
  @MaxLength(255)
  subject: string;

  @IsOptional()
  @IsEnum(TicketCategory)
  category?: TicketCategory;

  @IsString()
  @MinLength(1)
  message: string;
}
