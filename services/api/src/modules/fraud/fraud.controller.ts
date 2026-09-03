import { Controller, Get } from '@nestjs/common';

@Controller('fraud')
export class FraudController {
  @Get('_status')
  status() {
    return { module: 'fraud', status: 'not_implemented' };
  }
}
