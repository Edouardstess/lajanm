import { Module } from '@nestjs/common';
import { WalletController } from './wallet.controller';
import { WalletService } from './wallet.service';

/**
 * Placeholder module — wired into AppModule so the module boundary exists
 * from the start (see docs/architecture.md), but the actual business logic
 * for this domain has not been implemented yet.
 */
@Module({
  controllers: [WalletController],
  providers: [WalletService],
  exports: [WalletService],
})
export class WalletModule {}
