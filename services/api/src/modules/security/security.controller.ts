import { Body, Controller, Post, UseGuards } from '@nestjs/common';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { RequestOtpDto } from './dto/request-otp.dto';
import { SecurityService } from './security.service';

@UseGuards(JwtAuthGuard)
@Controller('security')
export class SecurityController {
  constructor(private readonly securityService: SecurityService) {}

  @Post('otp/request')
  requestOtp(@CurrentUser() user: { id: string }, @Body() dto: RequestOtpDto) {
    return this.securityService.requestOtp(user.id, dto.purpose);
  }
}
