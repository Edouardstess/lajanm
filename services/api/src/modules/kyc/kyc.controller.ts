import { Controller, Get } from '@nestjs/common';

@Controller('kyc')
export class KycController {
  @Get('_status')
  status() {
    return { module: 'kyc', status: 'not_implemented' };
  }
}
