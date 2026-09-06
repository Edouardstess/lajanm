import { Global, Module } from '@nestjs/common';
import { RateLimitService } from './rate-limit.service';

/**
 * Global : le compteur ouvre une connexion Redis, et il n'y a aucune
 * raison d'en ouvrir une par module qui limite quelque chose.
 */
@Global()
@Module({
  providers: [RateLimitService],
  exports: [RateLimitService],
})
export class RateLimitModule {}
