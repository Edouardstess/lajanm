import { UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import * as argon2 from 'argon2';
import { randomUUID } from 'crypto';
import { Repository } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { AdminAuthService } from './admin-auth.service';
import { AdminRole, AdminUser } from './entities/admin-user.entity';

function createInMemoryRepo() {
  const rows: AdminUser[] = [];
  return {
    rows,
    findOneBy: jest.fn(async (where: Partial<AdminUser>) => {
      const keys = Object.keys(where) as Array<keyof AdminUser>;
      return rows.find((r) => keys.every((k) => r[k] === where[k])) ?? null;
    }),
  } as unknown as Repository<AdminUser> & { rows: AdminUser[] };
}

describe('AdminAuthService', () => {
  let admins: ReturnType<typeof createInMemoryRepo>;
  let service: AdminAuthService;
  let auditService: { record: jest.Mock };

  beforeEach(async () => {
    admins = createInMemoryRepo();
    admins.rows.push({
      id: randomUUID(),
      email: 'ops@lajanm.example',
      passwordHash: await argon2.hash('correct-password'),
      role: AdminRole.OPERATOR,
      fullName: 'Ops',
      createdAt: new Date(),
    });
    auditService = { record: jest.fn().mockResolvedValue(undefined) };
    const jwtService = new JwtService({ secret: 'test-secret' });
    service = new AdminAuthService(admins, jwtService, auditService as unknown as AuditService);
  });

  it('logs in with correct credentials and issues a token carrying the admin role', async () => {
    const result = await service.login({ email: 'ops@lajanm.example', password: 'correct-password' });
    expect(result.accessToken).toBeTruthy();
    expect(result.admin.role).toBe(AdminRole.OPERATOR);
  });

  it('rejects a wrong password without revealing whether the email exists', async () => {
    await expect(
      service.login({ email: 'ops@lajanm.example', password: 'wrong-password' }),
    ).rejects.toThrow(UnauthorizedException);
    await expect(
      service.login({ email: 'nobody@lajanm.example', password: 'wrong-password' }),
    ).rejects.toThrow(UnauthorizedException);
  });

  it('issues a token distinguishable from a regular user token via the type claim', async () => {
    const result = await service.login({ email: 'ops@lajanm.example', password: 'correct-password' });
    const jwtService = new JwtService({ secret: 'test-secret' });
    const payload = await jwtService.verifyAsync<{ type?: string }>(result.accessToken);
    expect(payload.type).toBe('admin');
  });
});
