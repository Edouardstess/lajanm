import { IsEnum, IsOptional } from 'class-validator';
import { PaginationQueryDto } from '../../../common/dto/pagination-query.dto';
import { TicketStatus } from '../entities/support-ticket.entity';

/**
 * The operator queue's filter, on top of the shared paging fields. The
 * global ValidationPipe runs with forbidNonWhitelisted, so `status` has to
 * be declared here rather than read loosely off the query string.
 */
export class TicketQueueQueryDto extends PaginationQueryDto {
  @IsOptional()
  @IsEnum(TicketStatus)
  status?: TicketStatus;
}
