import { Controller, Get } from '@nestjs/common';

@Controller('compliance')
export class ComplianceController {
  @Get('_status')
  status() {
    return { module: 'compliance', status: 'not_implemented' };
  }
}
