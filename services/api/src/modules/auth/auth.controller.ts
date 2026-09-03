import { Controller, Get } from '@nestjs/common';

@Controller('auth')
export class AuthController {
  @Get('_status')
  status() {
    return { module: 'auth', status: 'not_implemented' };
  }
}
