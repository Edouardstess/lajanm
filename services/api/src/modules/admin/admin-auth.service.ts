import { Injectable, UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { InjectRepository } from '@nestjs/typeorm';
import * as argon2 from 'argon2';
import { Repository } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { AdminLoginDto } from './dto/admin-login.dto';
import { AdminRole, AdminUser } from './entities/admin-user.entity';

export interface AdminAuthResult {
  accessToken: string;
  admin: { id: string; email: string; role: AdminRole; fullName: string | null };
}

@Injectable()
export class AdminAuthService {
  constructor(
    @InjectRepository(AdminUser) private readonly admins: Repository<AdminUser>,
    private readonly jwtService: JwtService,
    private readonly auditService: AuditService,
  ) {}

  async login(dto: AdminLoginDto): Promise<AdminAuthResult> {
    const admin = await this.admins.findOneBy({ email: dto.email });
    // Same error for "no such admin" and "wrong password" — never let this
    // endpoint be used to enumerate back-office accounts.
    if (!admin || !(await argon2.verify(admin.passwordHash, dto.password))) {
      throw new UnauthorizedException('Invalid email or password');
    }

    await this.auditService.record({
      action: 'admin.login',
      actorId: admin.id,
      actorType: 'admin',
      targetId: admin.id,
    });

    // `type: 'admin'` is what AdminJwtAuthGuard checks for — a regular
    // user's token (issued by AuthService, no such claim) is rejected
    // outright, and vice versa: the two token spaces don't overlap.
    const accessToken = await this.jwtService.signAsync({
      sub: admin.id,
      email: admin.email,
      role: admin.role,
      type: 'admin',
    });

    return {
      accessToken,
      admin: { id: admin.id, email: admin.email, role: admin.role, fullName: admin.fullName },
    };
  }
}
