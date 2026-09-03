import { Body, Controller, Get, Headers, Param, Post, Query, Req, UseGuards } from '@nestjs/common';
import type { RawBodyRequest } from '@nestjs/common';
import type { Request } from 'express';
import { PaginationQueryDto } from '../../common/dto/pagination-query.dto';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { InitiateTopupDto } from './dto/initiate-topup.dto';
import { TopupService } from './topup.service';

@Controller('topup')
export class TopupController {
  constructor(private readonly topupService: TopupService) {}

  @UseGuards(JwtAuthGuard)
  @Post('initiate')
  initiate(@CurrentUser() user: { id: string }, @Body() dto: InitiateTopupDto) {
    return this.topupService.initiate(user.id, dto);
  }

  @UseGuards(JwtAuthGuard)
  @Get('history')
  history(@CurrentUser() user: { id: string }, @Query() paging: PaginationQueryDto) {
    return this.topupService.history(user.id, paging);
  }

  @UseGuards(JwtAuthGuard)
  @Get(':id')
  getStatus(@CurrentUser() user: { id: string }, @Param('id') id: string) {
    return this.topupService.getStatus(user.id, id);
  }

  /**
   * MonCash webhook — deliberately NOT behind JwtAuthGuard (MonCash isn't
   * one of our users). Authenticity comes from the HMAC signature verified
   * inside TopupService.handleWebhook, not from a bearer token. Reads the
   * raw body (see main.ts's `rawBody: true`) because the signature is
   * computed over the exact bytes MonCash sent, not a re-serialized copy.
   */
  @Post('webhook')
  async webhook(@Req() request: RawBodyRequest<Request>, @Headers('x-moncash-signature') signature?: string) {
    const rawBody = request.rawBody?.toString('utf8') ?? '';
    await this.topupService.handleWebhook(rawBody, signature);
    return { received: true };
  }
}
