import { Body, Controller, Get, Param, Patch, Query, UseGuards } from '@nestjs/common';
import { PaginationQueryDto } from '../../common/dto/pagination-query.dto';
import { CurrentAdmin } from '../admin/decorators/current-admin.decorator';
import { AdminJwtAuthGuard } from '../admin/guards/admin-jwt-auth.guard';
import { ResolveFlagDto } from './dto/resolve-flag.dto';
import { FraudService } from './fraud.service';

@UseGuards(AdminJwtAuthGuard)
@Controller('fraud')
export class FraudController {
  constructor(private readonly fraudService: FraudService) {}

  @Get('flags')
  listOpenFlags(@Query() paging: PaginationQueryDto) {
    return this.fraudService.listOpenFlags(paging);
  }

  @Patch('flags/:id')
  resolve(
    @CurrentAdmin() admin: { id: string },
    @Param('id') flagId: string,
    @Body() dto: ResolveFlagDto,
  ) {
    return this.fraudService.resolve(flagId, admin.id, dto);
  }
}
