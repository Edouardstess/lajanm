import { Controller, Get } from '@nestjs/common';

@Controller('topup')
export class TopupController {
  @Get('_status')
  status() {
    return { module: 'topup', status: 'not_implemented' };
  }
}
