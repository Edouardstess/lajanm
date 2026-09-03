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
  const port = config.get<number>('PORT') ?? 3000;
  await app.listen(port);
}
bootstrap();
