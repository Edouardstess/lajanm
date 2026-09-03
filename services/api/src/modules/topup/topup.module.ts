import { Module } from '@nestjs/common';
import { TopupController } from './topup.controller';
import { TopupService } from './topup.service';

/**
 * Placeholder module — wired into AppModule so the module boundary exists
 * from the start (see docs/architecture.md), but the actual business logic
 * for this domain has not been implemented yet.
 */
@Module({
  controllers: [TopupController],
  providers: [TopupService],
  exports: [TopupService],
})
export class TopupModule {}
