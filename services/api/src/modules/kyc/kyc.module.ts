import { Module } from '@nestjs/common';
import { KycController } from './kyc.controller';
import { KycService } from './kyc.service';

/**
 * Placeholder module — wired into AppModule so the module boundary exists
 * from the start (see docs/architecture.md), but the actual business logic
 * for this domain has not been implemented yet.
 */
@Module({
  controllers: [KycController],
  providers: [KycService],
  exports: [KycService],
})
export class KycModule {}
