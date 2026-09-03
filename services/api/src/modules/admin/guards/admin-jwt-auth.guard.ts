import { CanActivate, ExecutionContext, Injectable, UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { Request } from 'express';
import { AdminRole } from '../entities/admin-user.entity';

export interface AdminRequest extends Request {
  admin: { id: string; email: string; role: AdminRole };
}

/**
 * Mirrors auth/guards/jwt-auth.guard.ts but for the admin token space:
 * same JWT_SECRET (one signing key for the whole API), but only a token
 * carrying `type: 'admin'` (set by AdminAuthService.login) is accepted —
 * a regular user's token is structurally rejected here, and an admin
 * token is equally rejected by the user-facing JwtAuthGuard, since
 * neither payload shape satisfies the other's check.
 */
@Injectable()
export class AdminJwtAuthGuard implements CanActivate {
  constructor(private readonly jwtService: JwtService) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const request = context.switchToHttp().getRequest<AdminRequest>();
    const token = this.extractToken(request);
    if (!token) throw new UnauthorizedException('Missing bearer token');

    try {
      const payload = await this.jwtService.verifyAsync<{
        sub: string;
        email: string;
        role: AdminRole;
        type?: string;
      }>(token);

      if (payload.type !== 'admin') {
        throw new UnauthorizedException('Not an admin token');
      }

      request.admin = { id: payload.sub, email: payload.email, role: payload.role };
      return true;
    } catch {
      throw new UnauthorizedException('Invalid or expired token');
    }
  }

  private extractToken(request: Request): string | null {
    const header = request.headers.authorization;
    if (!header?.startsWith('Bearer ')) return null;
    return header.slice('Bearer '.length);
  }
}
