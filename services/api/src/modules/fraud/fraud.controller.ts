import { Body, Controller, Get, Param, Patch, UseGuards } from '@nestjs/common';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { ResolveFlagDto } from './dto/resolve-flag.dto';
import { FraudService } from './fraud.service';

/**
 * No role check yet — same pattern as KycController's review queue:
 * authenticated only, until the back-office module adds operator/admin
 * RBAC in front of endpoints like this one.
 */
@UseGuards(JwtAuthGuard)
@Controller('fraud')
export class FraudController {
  constructor(private readonly fraudService: FraudService) {}

  @Get('flags')
  listOpenFlags() {
    return this.fraudService.listOpenFlags();
  }

  @Patch('flags/:id')
  resolve(
    @CurrentUser() user: { id: string },
    @Param('id') flagId: string,
    @Body() dto: ResolveFlagDto,
  ) {
    return this.fraudService.resolve(flagId, user.id, dto);
  }
}
