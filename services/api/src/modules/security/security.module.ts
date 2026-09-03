import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AuditModule } from '../audit/audit.module';
import { AuthModule } from '../auth/auth.module';
import { LedgerModule } from '../ledger/ledger.module';
import { OtpCode } from './entities/otp-code.entity';
import { SecurityController } from './security.controller';
import { SecurityService } from './security.service';
import { SmsService } from './sms.service';

@Module({
  imports: [TypeOrmModule.forFeature([OtpCode]), AuthModule, AuditModule, LedgerModule],
  controllers: [SecurityController],
  providers: [SecurityService, SmsService],
  exports: [SecurityService],
})
export class SecurityModule {}
