import { ExecutionContext, UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { AdminJwtAuthGuard } from './admin-jwt-auth.guard';
import { AdminRole } from '../entities/admin-user.entity';

function contextWithAuthHeader(header: string | undefined) {
  const request: { headers: Record<string, string | undefined>; admin?: unknown } = {
    headers: { authorization: header },
  };
  return {
    switchToHttp: () => ({ getRequest: () => request }),
  } as unknown as ExecutionContext;
}

describe('AdminJwtAuthGuard', () => {
  const jwtService = new JwtService({ secret: 'shared-test-secret' });
  const guard = new AdminJwtAuthGuard(jwtService);

  it('accepts a token carrying type: admin', async () => {
    const token = await jwtService.signAsync({
      sub: 'admin-1',
      email: 'ops@lajanm.example',
      role: AdminRole.OPERATOR,
      type: 'admin',
    });
    await expect(guard.canActivate(contextWithAuthHeader(`Bearer ${token}`))).resolves.toBe(true);
  });

  it('rejects a regular user token — same secret, but no type: admin claim', async () => {
    // This mirrors exactly what AuthService.login issues for a customer:
    // { sub, phone }, no `type` field at all.
    const userToken = await jwtService.signAsync({ sub: 'user-1', phone: '+50900000001' });
    await expect(guard.canActivate(contextWithAuthHeader(`Bearer ${userToken}`))).rejects.toThrow(
      UnauthorizedException,
    );
  });

  it('rejects a missing token', async () => {
    await expect(guard.canActivate(contextWithAuthHeader(undefined))).rejects.toThrow(
      UnauthorizedException,
    );
  });

  it('rejects a token signed with a different secret', async () => {
    const otherJwtService = new JwtService({ secret: 'a-different-secret' });
    const token = await otherJwtService.signAsync({ sub: 'admin-1', type: 'admin' });
    await expect(guard.canActivate(contextWithAuthHeader(`Bearer ${token}`))).rejects.toThrow(
      UnauthorizedException,
    );
  });
});
