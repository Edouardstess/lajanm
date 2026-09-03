import { Body, Controller, Get, Param, Patch, Post, UseGuards } from '@nestjs/common';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { DecideKycDto } from './dto/decide-kyc.dto';
import { SubmitKycDto } from './dto/submit-kyc.dto';
import { KycService } from './kyc.service';

@UseGuards(JwtAuthGuard)
@Controller('kyc')
export class KycController {
  constructor(private readonly kycService: KycService) {}

  @Post('submissions')
  submit(@CurrentUser() user: { id: string }, @Body() dto: SubmitKycDto) {
    return this.kycService.submit(user.id, dto);
  }

  @Get('submissions/me')
  findMine(@CurrentUser() user: { id: string }) {
    return this.kycService.findMine(user.id);
  }

  @Get('submissions/queue')
  listQueue() {
    return this.kycService.listQueue();
  }

  @Patch('submissions/:id/decision')
  decide(
    @CurrentUser() user: { id: string },
    @Param('id') submissionId: string,
    @Body() dto: DecideKycDto,
  ) {
    return this.kycService.decide(submissionId, user.id, dto);
  }
}
