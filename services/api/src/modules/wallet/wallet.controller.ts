import { Body, Controller, Get, Post, Query, UseGuards } from '@nestjs/common';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { HistoryQueryDto } from './dto/history-query.dto';
import { LookupRecipientDto } from './dto/lookup-recipient.dto';
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

  // Déclarée AVANT /transfer sans que l'ordre importe ici (chemins
  // littéraux distincts), mais placée près d'elle : c'est l'étape qui la
  // précède dans le parcours de l'utilisateur.
  @Get('recipients/lookup')
  lookupRecipient(@CurrentUser() user: { id: string }, @Query() dto: LookupRecipientDto) {
    return this.walletService.lookupRecipient(user.id, dto);
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
