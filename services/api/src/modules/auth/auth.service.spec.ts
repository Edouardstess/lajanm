import { ConflictException, UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { randomUUID } from 'crypto';
import { Repository } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { AuthService } from './auth.service';
import { DeviceSession } from './entities/device-session.entity';
import { User, UserTier } from './entities/user.entity';

/**
 * Minimal in-memory stand-in for a TypeORM Repository<T>, keyed by id.
 * `defaults` mirrors the @Column({ default: ... }) values TypeORM's real
 * repository.create() applies — without them, e.g. failedPinAttempts would
 * start as undefined instead of 0, silently breaking `+= 1` arithmetic.
 */
function matches<T extends object>(row: T, where: Partial<T>): boolean {
  return (Object.keys(where) as Array<keyof T>).every((key) => row[key] === where[key]);
}

function createInMemoryRepo<T extends { id?: string }>(defaults: Partial<T> = {}) {
  const rows: T[] = [];
  return {
    rows,
    create: jest.fn((data: Partial<T>) => ({ id: randomUUID(), ...defaults, ...data }) as T),
    save: jest.fn(async (entity: T) => {
      const index = rows.findIndex((r) => r.id === entity.id);
      if (index === -1) rows.push(entity);
      else rows[index] = entity;
      return entity;
    }),
    findOneBy: jest.fn(async (where: Partial<T>) => {
      return rows.find((r) => matches(r, where)) ?? null;
    }),
    findOneByOrFail: jest.fn(async (where: Partial<T>) => {
      const found = rows.find((r) => matches(r, where));
      if (!found) throw new Error('not found');
      return found;
    }),
    find: jest.fn(async (options?: { where?: Partial<T> }) => {
      if (!options?.where) return rows;
      return rows.filter((r) => matches(r, options.where!));
    }),
  } as unknown as Repository<T> & { rows: T[] };
}

// argon2 hashing is intentionally slow; the lockout test hashes/verifies
// several times.
jest.setTimeout(20000);

describe('AuthService', () => {
  let users: ReturnType<typeof createInMemoryRepo<User>>;
  let devices: ReturnType<typeof createInMemoryRepo<DeviceSession>>;
  let auditService: { record: jest.Mock };
  let service: AuthService;

  beforeEach(() => {
    users = createInMemoryRepo<User>({
      failedPinAttempts: 0,
      lockedUntil: null,
      fullName: null,
      email: null,
    });
    devices = createInMemoryRepo<DeviceSession>();
    auditService = { record: jest.fn().mockResolvedValue(undefined) };
    const jwtService = new JwtService({ secret: 'test-secret' });
    service = new AuthService(
      users,
      devices,
      jwtService,
      auditService as unknown as AuditService,
    );
  });

  it('registers a new user with a hashed PIN, never the raw PIN', async () => {
    const result = await service.register({ phone: '+50912345678', pin: '1234' });
    expect(result.tier).toBe(UserTier.BASIC);
    expect(users.rows[0].pinHash).not.toBe('1234');
    expect(users.rows[0].pinHash).toMatch(/^\$argon2/);
  });

  it('rejects registering the same phone number twice', async () => {
    await service.register({ phone: '+50912345678', pin: '1234' });
    await expect(service.register({ phone: '+50912345678', pin: '5678' })).rejects.toThrow(
      ConflictException,
    );
  });

  it('logs in with correct credentials and issues a device session', async () => {
    await service.register({ phone: '+50912345678', pin: '1234' });
    const result = await service.login({
      phone: '+50912345678',
      pin: '1234',
      deviceId: 'device-1',
    });
    expect(result.accessToken).toBeTruthy();
    expect(result.user.phone).toBe('+50912345678');
    expect(devices.rows).toHaveLength(1);
    expect(devices.rows[0].deviceId).toBe('device-1');
  });

  it('rejects an incorrect PIN without revealing whether the phone exists', async () => {
    await service.register({ phone: '+50912345678', pin: '1234' });
    await expect(
      service.login({ phone: '+50912345678', pin: '0000', deviceId: 'device-1' }),
    ).rejects.toThrow(UnauthorizedException);
    await expect(
      service.login({ phone: '+50900000000', pin: '0000', deviceId: 'device-1' }),
    ).rejects.toThrow(UnauthorizedException);
  });

  it('locks the account after 5 consecutive failed PIN attempts', async () => {
    await service.register({ phone: '+50912345678', pin: '1234' });

    for (let i = 0; i < 5; i += 1) {
      await expect(
        service.login({ phone: '+50912345678', pin: 'wrong', deviceId: 'device-1' }),
      ).rejects.toThrow(UnauthorizedException);
    }

    // Even the correct PIN is now rejected while the account is locked.
    await expect(
      service.login({ phone: '+50912345678', pin: '1234', deviceId: 'device-1' }),
    ).rejects.toThrow('Account temporarily locked, try again later');
  });
});
