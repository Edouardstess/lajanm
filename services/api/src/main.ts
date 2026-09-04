import { NestFactory } from '@nestjs/core';
import { ValidationPipe } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { AppModule } from './app.module';

async function bootstrap() {
  // rawBody: true keeps the exact request bytes available on req.rawBody,
  // needed by the MonCash webhook handler to verify the HMAC signature
  // over what MonCash actually sent (a re-serialized JSON.stringify of the
  // parsed body would not reliably match byte-for-byte).
  const app = await NestFactory.create(AppModule, { rawBody: true });
  app.useGlobalPipes(
    new ValidationPipe({
      whitelist: true,
      forbidNonWhitelisted: true,
      transform: true,
    }),
  );

  const config = app.get(ConfigService);

  // admin-web runs on its own origin (a different port even in local dev),
  // and the mobile app's WebView-based flows (MonCash redirect) may too —
  // without this, every browser-based caller is silently blocked by CORS
  // before a request ever reaches a controller. CORS_ORIGINS is a
  // comma-separated allowlist; unset means "reflect the request origin"
  // (open, for frictionless local dev) rather than silently defaulting to
  // a hardcoded origin that would break in every other environment —
  // tighten this to an explicit allowlist before any real deployment.
  const corsOrigins = config.get<string>('CORS_ORIGINS');
  app.enableCors({
    origin: corsOrigins ? corsOrigins.split(',').map((o) => o.trim()) : true,
    credentials: true,
  });

  const port = config.get<number>('PORT') ?? 3000;
  // L'hôte est explicite : sans lui, Node peut n'écouter que sur l'IPv6
  // du conteneur. Les hébergeurs (Render, Fly, Cloud Run...) sondent le
  // port sur 0.0.0.0 et concluent « aucun port ouvert », ce qui fait
  // échouer le déploiement alors que l'application a démarré normalement.
  await app.listen(port, '0.0.0.0');
}
bootstrap();
