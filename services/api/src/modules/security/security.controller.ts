import { Controller, Get } from '@nestjs/common';

@Controller('security')
export class SecurityController {
  @Get('_status')
  status() {
    return { module: 'security', status: 'not_implemented' };
  }
}
