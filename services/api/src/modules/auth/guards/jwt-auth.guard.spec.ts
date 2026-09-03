import { ExecutionContext, UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { JwtAuthGuard } from './jwt-auth.guard';

function contextWithAuthHeader(header: string | undefined) {
  const request: { headers: Record<string, string | undefined>; user?: unknown } = {
    headers: { authorization: header },
  };
  return {
    switchToHttp: () => ({ getRequest: () => request }),
  } as unknown as ExecutionContext;
}

describe('JwtAuthGuard', () => {
  const jwtService = new JwtService({ secret: 'shared-test-secret' });
  const guard = new JwtAuthGuard(jwtService);

  it('accepts a regular user token', async () => {
    const token = await jwtService.signAsync({ sub: 'user-1', phone: '+50900000001' });
    await expect(guard.canActivate(contextWithAuthHeader(`Bearer ${token}`))).resolves.toBe(true);
  });

  it('rejects an admin token even though it verifies with the same secret', async () => {
    // Regression test: this exact scenario let an admin token pass
    // JwtAuthGuard and hit /wallet/balance, silently treating the admin's
    // id as a customer id, before the `type` check was added — found by
    // manually exercising the two token spaces against each other's
    // routes on a running instance, not by a test that existed at the
    // time.
    const adminToken = await jwtService.signAsync({
      sub: 'admin-1',
      email: 'ops@lajanm.example',
      role: 'admin',
      type: 'admin',
    });
    await expect(guard.canActivate(contextWithAuthHeader(`Bearer ${adminToken}`))).rejects.toThrow(
      UnauthorizedException,
    );
  });

  it('rejects a missing token', async () => {
    await expect(guard.canActivate(contextWithAuthHeader(undefined))).rejects.toThrow(
      UnauthorizedException,
    );
  });
});
