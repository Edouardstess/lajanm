import { BullModule } from '@nestjs/bullmq';
import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AuditModule } from '../audit/audit.module';
import { AuthModule } from '../auth/auth.module';
import { LedgerModule } from '../ledger/ledger.module';
import { TopupTransaction } from './entities/topup-transaction.entity';
import { MonCashClient } from './moncash-client.service';
import { TopupInitiationProcessor } from './processors/topup-initiation.processor';
import { TOPUP_INITIATION_QUEUE } from './topup.service';
import { TopupController } from './topup.controller';
import { TopupService } from './topup.service';

@Module({
  imports: [
    TypeOrmModule.forFeature([TopupTransaction]),
    BullModule.registerQueue({ name: TOPUP_INITIATION_QUEUE }),
    AuthModule,
    AuditModule,
    LedgerModule,
  ],
  controllers: [TopupController],
  providers: [TopupService, MonCashClient, TopupInitiationProcessor],
  exports: [TopupService],
})
export class TopupModule {}
