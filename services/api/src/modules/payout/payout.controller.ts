import { Body, Controller, Get, Post, Query, UseGuards } from '@nestjs/common';
import { PaginationQueryDto } from '../../common/dto/pagination-query.dto';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { InitiatePayoutDto } from './dto/initiate-payout.dto';
import { PayoutService } from './payout.service';

@UseGuards(JwtAuthGuard)
@Controller('payout')
export class PayoutController {
  constructor(private readonly payoutService: PayoutService) {}

  @Get('limit')
  getLimit() {
    return { maxAmountHTG: this.payoutService.getMaxAmountHTG() };
  }

  @Post('initiate')
  initiate(@CurrentUser() user: { id: string }, @Body() dto: InitiatePayoutDto) {
    return this.payoutService.initiate(user.id, dto);
  }

  @Get('history')
  history(@CurrentUser() user: { id: string }, @Query() paging: PaginationQueryDto) {
    return this.payoutService.history(user.id, paging);
  }
}
