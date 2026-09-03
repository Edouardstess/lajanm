import { ConflictException, Injectable, UnauthorizedException } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { JwtService } from '@nestjs/jwt';
import * as argon2 from 'argon2';
import { Repository } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { ChangePinDto } from './dto/change-pin.dto';
import { LoginDto } from './dto/login.dto';
import { RegisterDto } from './dto/register.dto';
import { UpdateProfileDto } from './dto/update-profile.dto';
import { DeviceSession } from './entities/device-session.entity';
import { User, UserTier } from './entities/user.entity';

export interface AuthResult {
  accessToken: string;
  user: Pick<User, 'id' | 'phone' | 'tier' | 'fullName' | 'email'>;
}

@Injectable()
export class AuthService {
  constructor(
    @InjectRepository(User) private readonly users: Repository<User>,
    @InjectRepository(DeviceSession) private readonly devices: Repository<DeviceSession>,
    private readonly jwtService: JwtService,
    private readonly auditService: AuditService,
  ) {}

  async register(dto: RegisterDto): Promise<Pick<User, 'id' | 'phone' | 'tier'>> {
    const existing = await this.users.findOneBy({ phone: dto.phone });
    if (existing) {
      throw new ConflictException('A user with this phone number already exists');
    }

    const pinHash = await argon2.hash(dto.pin);
    const user = await this.users.save(
      this.users.create({ phone: dto.phone, pinHash, tier: UserTier.BASIC }),
    );

    await this.auditService.record({
      action: 'auth.register',
      actorId: user.id,
      actorType: 'user',
      targetId: user.id,
    });

    return { id: user.id, phone: user.phone, tier: user.tier };
  }

  async login(dto: LoginDto): Promise<AuthResult> {
    const user = await this.users.findOneBy({ phone: dto.phone });
    // Same error for "no such user" and "wrong PIN" — never let an
    // attacker use this endpoint to enumerate registered phone numbers.
    if (!user || !(await argon2.verify(user.pinHash, dto.pin))) {
      if (user) {
        await this.registerFailedPinAttempt(user);
      }
      throw new UnauthorizedException('Invalid phone number or PIN');
    }

    if (user.lockedUntil && user.lockedUntil > new Date()) {
      throw new UnauthorizedException('Account temporarily locked, try again later');
    }

    if (user.failedPinAttempts > 0) {
      user.failedPinAttempts = 0;
      user.lockedUntil = null;
      await this.users.save(user);
    }

    await this.upsertDeviceSession(user.id, dto.deviceId, dto.deviceName);

    await this.auditService.record({
      action: 'auth.login',
      actorId: user.id,
      actorType: 'user',
      targetId: user.id,
      metadata: { deviceId: dto.deviceId },
    });

    const accessToken = await this.jwtService.signAsync({ sub: user.id, phone: user.phone });
    return {
      accessToken,
      user: { id: user.id, phone: user.phone, tier: user.tier, fullName: user.fullName, email: user.email },
    };
  }

  async changePin(userId: string, dto: ChangePinDto): Promise<void> {
    const user = await this.users.findOneByOrFail({ id: userId });
    if (!(await argon2.verify(user.pinHash, dto.currentPin))) {
      throw new UnauthorizedException('Current PIN is incorrect');
    }

    user.pinHash = await argon2.hash(dto.newPin);
    await this.users.save(user);

    await this.auditService.record({
      action: 'auth.pin_changed',
      actorId: userId,
      actorType: 'user',
      targetId: userId,
    });
  }

  async updateProfile(
    userId: string,
    dto: UpdateProfileDto,
  ): Promise<Pick<User, 'id' | 'phone' | 'tier' | 'fullName' | 'email'>> {
    const user = await this.users.findOneByOrFail({ id: userId });
    if (dto.fullName !== undefined) user.fullName = dto.fullName;
    if (dto.email !== undefined) user.email = dto.email;
    const saved = await this.users.save(user);

    await this.auditService.record({
      action: 'auth.profile_updated',
      actorId: userId,
      actorType: 'user',
      targetId: userId,
    });

    // Never return pinHash, even hashed — the profile endpoint has no
    // business exposing it.
    return { id: saved.id, phone: saved.phone, tier: saved.tier, fullName: saved.fullName, email: saved.email };
  }

  async listDevices(userId: string): Promise<DeviceSession[]> {
    return this.devices.find({ where: { userId }, order: { lastSeenAt: 'DESC' } });
  }

  async revokeDevice(userId: string, deviceSessionId: string): Promise<void> {
    const session = await this.devices.findOneByOrFail({ id: deviceSessionId, userId });
    session.revokedAt = new Date();
    await this.devices.save(session);

    await this.auditService.record({
      action: 'auth.device_revoked',
      actorId: userId,
      actorType: 'user',
      targetId: session.id,
    });
  }

  private async upsertDeviceSession(
    userId: string,
    deviceId: string,
    deviceName?: string,
  ): Promise<void> {
    const existing = await this.devices.findOneBy({ userId, deviceId });
    if (existing) {
      existing.lastSeenAt = new Date();
      existing.revokedAt = null;
      if (deviceName) existing.deviceName = deviceName;
      await this.devices.save(existing);
      return;
    }

    await this.devices.save(
      this.devices.create({
        userId,
        deviceId,
        deviceName: deviceName ?? null,
        lastSeenAt: new Date(),
      }),
    );
  }

  /**
   * NF-05/NF-08: lock the account for 15 minutes after 5 consecutive failed
   * PIN attempts. Tracked here (Module 1) because it touches the User row
   * directly on every login attempt; the broader security module (fraud
   * velocity rules, OTP) builds on top of this in a later phase.
   */
  private async registerFailedPinAttempt(user: User): Promise<void> {
    user.failedPinAttempts += 1;
    if (user.failedPinAttempts >= 5) {
      user.lockedUntil = new Date(Date.now() + 15 * 60 * 1000);
    }
    await this.users.save(user);

    await this.auditService.record({
      action: 'auth.pin_failed',
      actorId: user.id,
      actorType: 'user',
      targetId: user.id,
      metadata: { failedPinAttempts: user.failedPinAttempts },
    });
  }
}
