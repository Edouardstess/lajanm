import { Controller, Get } from '@nestjs/common';

@Controller('payout')
export class PayoutController {
  @Get('_status')
  status() {
    return { module: 'payout', status: 'not_implemented' };
  }
}
