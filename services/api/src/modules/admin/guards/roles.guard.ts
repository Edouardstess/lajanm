import { CanActivate, ExecutionContext, ForbiddenException, Injectable } from '@nestjs/common';
import { Reflector } from '@nestjs/core';
import { ROLES_KEY } from '../decorators/roles.decorator';
import { AdminRequest } from './admin-jwt-auth.guard';
import { AdminRole } from '../entities/admin-user.entity';

/**
 * Must run after AdminJwtAuthGuard (which populates request.admin) — see
 * @Roles usage on controllers. No @Roles decorator means "any
 * authenticated admin", not "no admin required": this guard never
 * substitutes for AdminJwtAuthGuard, only narrows it further.
 */
@Injectable()
export class RolesGuard implements CanActivate {
  constructor(private readonly reflector: Reflector) {}

  canActivate(context: ExecutionContext): boolean {
    const requiredRoles = this.reflector.getAllAndOverride<AdminRole[]>(ROLES_KEY, [
      context.getHandler(),
      context.getClass(),
    ]);
    if (!requiredRoles || requiredRoles.length === 0) return true;

    const request = context.switchToHttp().getRequest<AdminRequest>();
    if (!requiredRoles.includes(request.admin.role)) {
      throw new ForbiddenException('Insufficient role for this action');
    }
    return true;
  }
}
