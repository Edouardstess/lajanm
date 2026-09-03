import { Module } from '@nestjs/common';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { BullModule } from '@nestjs/bullmq';
import { validate } from './config/env.validation';
import { DatabaseModule } from './database/database.module';
import { AppController } from './app.controller';
import { AuditModule } from './modules/audit/audit.module';
import { AuthModule } from './modules/auth/auth.module';
import { KycModule } from './modules/kyc/kyc.module';
import { LedgerModule } from './modules/ledger/ledger.module';
import { TopupModule } from './modules/topup/topup.module';
import { PayoutModule } from './modules/payout/payout.module';
import { WalletModule } from './modules/wallet/wallet.module';
import { SecurityModule } from './modules/security/security.module';
import { FraudModule } from './modules/fraud/fraud.module';
import { ComplianceModule } from './modules/compliance/compliance.module';
import { SupportModule } from './modules/support/support.module';

@Module({
  imports: [
    ConfigModule.forRoot({
      isGlobal: true,
      validate,
      // No env file is loaded implicitly here: dev/staging/prod each supply
      // their own DATABASE_URL/REDIS_URL/JWT_SECRET via the deployment
      // environment (see infra/ and docker-compose.yml for local dev), so
      // there is no risk of one environment's config silently leaking into
      // another (NF-24).
    }),
    BullModule.forRootAsync({
      imports: [ConfigModule],
      inject: [ConfigService],
      useFactory: (config: ConfigService) => ({
        connection: { url: config.get<string>('REDIS_URL') },
      }),
    }),
    DatabaseModule,
    AuditModule,
    AuthModule,
    KycModule,
    LedgerModule,
    TopupModule,
    PayoutModule,
    WalletModule,
    SecurityModule,
    FraudModule,
    ComplianceModule,
    SupportModule,
  ],
  controllers: [AppController],
})
export class AppModule {}
