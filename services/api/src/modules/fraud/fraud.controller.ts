import { Body, Controller, Get, Param, Patch, UseGuards } from '@nestjs/common';
import { CurrentAdmin } from '../admin/decorators/current-admin.decorator';
import { AdminJwtAuthGuard } from '../admin/guards/admin-jwt-auth.guard';
import { ResolveFlagDto } from './dto/resolve-flag.dto';
import { FraudService } from './fraud.service';

@UseGuards(AdminJwtAuthGuard)
@Controller('fraud')
export class FraudController {
  constructor(private readonly fraudService: FraudService) {}

  @Get('flags')
  listOpenFlags() {
    return this.fraudService.listOpenFlags();
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
