import { createParamDecorator, ExecutionContext } from '@nestjs/common';
import { AdminRequest } from '../guards/admin-jwt-auth.guard';

export const CurrentAdmin = createParamDecorator(
  (_data: unknown, ctx: ExecutionContext): AdminRequest['admin'] => {
    const request = ctx.switchToHttp().getRequest<AdminRequest>();
    return request.admin;
  },
);
