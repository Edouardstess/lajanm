import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AdminModule } from '../admin/admin.module';
import { AuditModule } from '../audit/audit.module';
import { AuthModule } from '../auth/auth.module';
import { LedgerModule } from '../ledger/ledger.module';
import { ComplianceController } from './compliance.controller';
import { ComplianceService } from './compliance.service';
import { Dispute } from './entities/dispute.entity';
import { SuspiciousActivityReport } from './entities/suspicious-activity-report.entity';

@Module({
  imports: [
    TypeOrmModule.forFeature([Dispute, SuspiciousActivityReport]),
    AuthModule,
    AdminModule,
    AuditModule,
    LedgerModule,
  ],
  controllers: [ComplianceController],
  providers: [ComplianceService],
  exports: [ComplianceService],
})
export class ComplianceModule {}
