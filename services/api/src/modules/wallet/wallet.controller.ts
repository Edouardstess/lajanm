import { Body, Controller, Get, Post, Query, UseGuards } from '@nestjs/common';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { HistoryQueryDto } from './dto/history-query.dto';
import { TransferDto } from './dto/transfer.dto';
import { WalletService } from './wallet.service';

@UseGuards(JwtAuthGuard)
@Controller('wallet')
export class WalletController {
  constructor(private readonly walletService: WalletService) {}

  @Get('balance')
  getBalance(@CurrentUser() user: { id: string }) {
    return this.walletService.getBalance(user.id);
  }

  @Post('transfer')
  transfer(@CurrentUser() user: { id: string }, @Body() dto: TransferDto) {
    return this.walletService.transfer(user.id, dto);
  }

  @Get('history')
  history(@CurrentUser() user: { id: string }, @Query() query: HistoryQueryDto) {
    return this.walletService.history(user.id, query);
  }
}
