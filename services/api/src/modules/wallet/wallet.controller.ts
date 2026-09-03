import { Controller, Get } from '@nestjs/common';

@Controller('wallet')
export class WalletController {
  @Get('_status')
  status() {
    return { module: 'wallet', status: 'not_implemented' };
  }
}
