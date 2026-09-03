import { Body, Controller, Get, Param, Patch, Post, UseGuards } from '@nestjs/common';
import { CurrentAdmin } from '../admin/decorators/current-admin.decorator';
import { AdminJwtAuthGuard } from '../admin/guards/admin-jwt-auth.guard';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { DecideKycDto } from './dto/decide-kyc.dto';
import { SubmitKycDto } from './dto/submit-kyc.dto';
import { KycService } from './kyc.service';

@Controller('kyc')
export class KycController {
  constructor(private readonly kycService: KycService) {}

  @UseGuards(JwtAuthGuard)
  @Post('submissions')
  submit(@CurrentUser() user: { id: string }, @Body() dto: SubmitKycDto) {
    return this.kycService.submit(user.id, dto);
  }

  @UseGuards(JwtAuthGuard)
  @Get('submissions/me')
  findMine(@CurrentUser() user: { id: string }) {
    return this.kycService.findMine(user.id);
  }

  @UseGuards(AdminJwtAuthGuard)
  @Get('submissions/queue')
  listQueue() {
    return this.kycService.listQueue();
  }

  @UseGuards(AdminJwtAuthGuard)
  @Patch('submissions/:id/decision')
  decide(
    @CurrentAdmin() admin: { id: string },
    @Param('id') submissionId: string,
    @Body() dto: DecideKycDto,
  ) {
    return this.kycService.decide(submissionId, admin.id, dto);
  }
}
