import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AuditModule } from '../audit/audit.module';
import { AuthModule } from '../auth/auth.module';
import { LedgerModule } from '../ledger/ledger.module';
import { NotificationsModule } from '../notifications/notifications.module';
import { TopupModule } from '../topup/topup.module';
import { PayoutTransaction } from './entities/payout-transaction.entity';
import { PayoutController } from './payout.controller';
import { PayoutService } from './payout.service';

@Module({
  imports: [
    TypeOrmModule.forFeature([PayoutTransaction]),
    AuthModule,
    AuditModule,
    LedgerModule,
    NotificationsModule,
    TopupModule,
  ],
  controllers: [PayoutController],
  providers: [PayoutService],
  exports: [PayoutService],
})
export class PayoutModule {}
