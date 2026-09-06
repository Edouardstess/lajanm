import {
  HttpException,
  HttpStatus,
  Injectable,
  Logger,
  OnModuleDestroy,
  ServiceUnavailableException,
} from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import Redis from 'ioredis';

/**
 * Compteur de requêtes par fenêtre glissante, dans Redis.
 *
 * Redis plutôt qu'un compteur en mémoire parce qu'un compteur en mémoire
 * ne compte que pour l'instance qui l'héberge : dès qu'il y en a deux, la
 * limite est doublée sans que personne ne s'en aperçoive.
 *
 * En cas de panne Redis, le service REFUSE (voir `consume`). C'est
 * délibéré : ce compteur protège contre l'énumération d'un annuaire de
 * numéros, et un contrôle anti-abus qui s'ouvre quand on coupe Redis
 * n'est pas un contrôle. Les appelants doivent donc traiter l'échec
 * comme un dégradé acceptable, pas comme une panne bloquante.
 */
@Injectable()
export class RateLimitService implements OnModuleDestroy {
  private readonly logger = new Logger(RateLimitService.name);
  private readonly redis: Redis;

  constructor(config: ConfigService) {
    this.redis = new Redis(config.getOrThrow<string>('REDIS_URL'), {
      // Sans cela, ioredis met les commandes en file pendant une panne et
      // l'appelant attend indéfiniment au lieu de recevoir une erreur.
      maxRetriesPerRequest: 1,
      enableOfflineQueue: false,
      lazyConnect: false,
    });
    // Un `error` non écouté sur un client ioredis termine le processus.
    this.redis.on('error', (err) => this.logger.warn(`Redis: ${err.message}`));
  }

  /**
   * Compte un appel et refuse au-delà de `limit` sur `windowSeconds`.
   *
   * @throws HttpException 429 au-delà de la limite
   * @throws ServiceUnavailableException si Redis ne répond pas
   */
  async consume(key: string, limit: number, windowSeconds: number): Promise<void> {
    const redisKey = `ratelimit:${key}`;
    let count: number;

    try {
      // INCR puis EXPIRE dans le même aller-retour. EXPIRE est posé à
      // chaque appel plutôt qu'au seul premier : une clé sans TTL, si
      // l'EXPIRE initial s'était perdu, bloquerait l'utilisateur pour
      // toujours.
      const [incr] = (await this.redis
        .multi()
        .incr(redisKey)
        .expire(redisKey, windowSeconds)
        .exec()) as Array<[Error | null, number]>;

      if (incr[0]) throw incr[0];
      count = incr[1];
    } catch (error) {
      this.logger.error(`Compteur indisponible pour ${key}: ${(error as Error).message}`);
      throw new ServiceUnavailableException('Rate limiting is unavailable, try again shortly');
    }

    if (count > limit) {
      throw new HttpException(
        { statusCode: HttpStatus.TOO_MANY_REQUESTS, message: 'Too many requests, slow down' },
        HttpStatus.TOO_MANY_REQUESTS,
      );
    }
  }

  async onModuleDestroy(): Promise<void> {
    await this.redis.quit().catch(() => {
      // Fermeture au mieux : le processus s'arrête de toute façon.
    });
  }
}
