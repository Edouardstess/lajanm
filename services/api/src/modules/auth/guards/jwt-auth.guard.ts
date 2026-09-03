import { CanActivate, ExecutionContext, Injectable, UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { Request } from 'express';

export interface AuthenticatedRequest extends Request {
  user: { id: string; phone: string };
}

@Injectable()
export class JwtAuthGuard implements CanActivate {
  constructor(private readonly jwtService: JwtService) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const request = context.switchToHttp().getRequest<AuthenticatedRequest>();
    const token = this.extractToken(request);
    if (!token) {
      throw new UnauthorizedException('Missing bearer token');
    }

    try {
      const payload = await this.jwtService.verifyAsync<{ sub: string; phone: string; type?: string }>(
        token,
      );
      // Both token spaces are signed with the same JWT_SECRET, so a valid
      // signature alone doesn't prove this is a customer token — an admin
      // token (see AdminAuthService.login) verifies just as validly here
      // otherwise, and would silently be treated as some user's session
      // (payload.sub would resolve to an admin_users id, not a users id).
      // The `type: 'admin'` claim is what tells the two apart.
      if (payload.type === 'admin') {
        throw new UnauthorizedException('Admin tokens cannot be used on customer routes');
      }
      request.user = { id: payload.sub, phone: payload.phone };
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
