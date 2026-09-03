import { Controller, Get } from '@nestjs/common';

@Controller('support')
export class SupportController {
  @Get('_status')
  status() {
    return { module: 'support', status: 'not_implemented' };
  }
}
