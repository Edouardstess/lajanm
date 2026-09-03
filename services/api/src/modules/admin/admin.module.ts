import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AuditModule } from '../audit/audit.module';
import { AuthModule } from '../auth/auth.module';
import { AdminAuthController } from './admin-auth.controller';
import { AdminAuthService } from './admin-auth.service';
import { AdminUser } from './entities/admin-user.entity';
import { AdminJwtAuthGuard } from './guards/admin-jwt-auth.guard';
import { RolesGuard } from './guards/roles.guard';

@Module({
  // AuthModule is imported only for its exported JwtModule (JwtService) —
  // admin auth is otherwise fully separate from user auth (see AdminUser
  // entity doc). It's re-exported here too: AdminJwtAuthGuard needs
  // JwtService, and any module that imports AdminModule to use that guard
  // (KycModule, FraudModule, ComplianceModule, ...) needs JwtService
  // visible in ITS OWN scope for Nest to construct the guard there — a
  // module merely exporting an already-built provider doesn't make that
  // provider's own dependencies visible to importers.
  imports: [TypeOrmModule.forFeature([AdminUser]), AuthModule, AuditModule],
  controllers: [AdminAuthController],
  providers: [AdminAuthService, AdminJwtAuthGuard, RolesGuard],
  exports: [AdminAuthService, AdminJwtAuthGuard, RolesGuard, TypeOrmModule, AuthModule],
})
export class AdminModule {}
