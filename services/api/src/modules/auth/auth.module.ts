import { Module } from '@nestjs/common';
import { AuthController } from './auth.controller';
import { AuthService } from './auth.service';

/**
 * Placeholder module — wired into AppModule so the module boundary exists
 * from the start (see docs/architecture.md), but the actual business logic
 * for this domain has not been implemented yet.
 */
@Module({
  controllers: [AuthController],
  providers: [AuthService],
  exports: [AuthService],
})
export class AuthModule {}
