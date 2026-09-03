import { plainToInstance } from 'class-transformer';
import { IsEnum, IsInt, IsOptional, IsString, Max, Min, validateSync } from 'class-validator';

enum Environment {
  Development = 'development',
  Staging = 'staging',
  Production = 'production',
  Test = 'test',
}

/**
 * Every environment must supply its own, distinct set of credentials.
 * There is no shared default: a missing var fails startup rather than
 * silently falling back to another environment's value.
 */
class EnvironmentVariables {
  @IsEnum(Environment)
  NODE_ENV: Environment;

  @IsInt()
  @Min(1)
  @Max(65535)
  PORT: number;

  @IsString()
  DATABASE_URL: string;

  @IsString()
  REDIS_URL: string;

  @IsString()
  JWT_SECRET: string;

  // MonCash integration: optional at boot (so the app still starts in an
  // environment that hasn't configured payments yet). When missing,
  // MonCashClient treats every call as MonCash-unavailable, which routes
  // through the same honest "pending, retrying" path as a real outage —
  // never a silent fake success. See MonCashClient.createPayment.
  @IsOptional()
  @IsString()
  MONCASH_BASE_URL?: string;

  @IsOptional()
  @IsString()
  MONCASH_CLIENT_ID?: string;

  @IsOptional()
  @IsString()
  MONCASH_CLIENT_SECRET?: string;

  @IsOptional()
  @IsString()
  MONCASH_WEBHOOK_SECRET?: string;

  // Regulatory per-transaction payout cap (BRH Circular n°121: 100,000
  // HTG). Configurable rather than hardcoded so it can be adjusted if the
  // regulation changes, but ACTIVE BY DEFAULT from the MVP — see
  // PayoutService and README's governance note. Defaults to 100000 when
  // unset (falsy check below, not ?? on the raw string, since env vars
  // arrive as strings and an empty string is falsy too).
  @IsOptional()
  @IsInt()
  @Min(1)
  PAYOUT_MAX_AMOUNT_HTG?: number;
}

export function validate(config: Record<string, unknown>) {
  const validatedConfig = plainToInstance(EnvironmentVariables, config, {
    enableImplicitConversion: true,
  });
  const errors = validateSync(validatedConfig, { skipMissingProperties: false });

  if (errors.length > 0) {
    throw new Error(
      `Invalid environment configuration:\n${errors
        .map((e) => Object.values(e.constraints ?? {}).join(', '))
        .join('\n')}`,
    );
  }
  return validatedConfig;
}
