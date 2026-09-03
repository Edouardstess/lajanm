import { Body, Controller, Get, Param, Patch, Post, UseGuards } from '@nestjs/common';
import { CurrentAdmin } from '../admin/decorators/current-admin.decorator';
import { AdminJwtAuthGuard } from '../admin/guards/admin-jwt-auth.guard';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { ComplianceService } from './compliance.service';
import { CreateDisputeDto } from './dto/create-dispute.dto';
import { CreateSarDto } from './dto/create-sar.dto';
import { UpdateDisputeDto } from './dto/update-dispute.dto';

@Controller('compliance')
export class ComplianceController {
  constructor(private readonly complianceService: ComplianceService) {}

  // --- Admin/operator endpoints ---

  @UseGuards(AdminJwtAuthGuard)
  @Get('reconciliation')
  getReconciliation() {
    return this.complianceService.getReconciliation();
  }

  @UseGuards(AdminJwtAuthGuard)
  @Get('disputes')
  listAllDisputes() {
    return this.complianceService.listAllDisputes();
  }

  @UseGuards(AdminJwtAuthGuard)
  @Patch('disputes/:id')
  updateDispute(
    @CurrentAdmin() admin: { id: string },
    @Param('id') disputeId: string,
    @Body() dto: UpdateDisputeDto,
  ) {
    return this.complianceService.updateDispute(disputeId, admin.id, dto);
  }

  @UseGuards(AdminJwtAuthGuard)
  @Post('sar')
  createSar(@CurrentAdmin() admin: { id: string }, @Body() dto: CreateSarDto) {
    return this.complianceService.createSar(admin.id, dto);
  }

  @UseGuards(AdminJwtAuthGuard)
  @Get('sar')
  listSars() {
    return this.complianceService.listSars();
  }

  // --- Customer-facing endpoints ---

  @UseGuards(JwtAuthGuard)
  @Post('disputes')
  createDispute(@CurrentUser() user: { id: string }, @Body() dto: CreateDisputeDto) {
    return this.complianceService.createDispute(user.id, dto);
  }

  @UseGuards(JwtAuthGuard)
  @Get('disputes/me')
  listMyDisputes(@CurrentUser() user: { id: string }) {
    return this.complianceService.listMyDisputes(user.id);
  }
}
