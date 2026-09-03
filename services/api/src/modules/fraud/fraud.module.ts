import { Module } from '@nestjs/common';
import { FraudController } from './fraud.controller';
import { FraudService } from './fraud.service';

/**
 * Placeholder module — wired into AppModule so the module boundary exists
 * from the start (see docs/architecture.md), but the actual business logic
 * for this domain has not been implemented yet.
 */
@Module({
  controllers: [FraudController],
  providers: [FraudService],
  exports: [FraudService],
})
export class FraudModule {}
