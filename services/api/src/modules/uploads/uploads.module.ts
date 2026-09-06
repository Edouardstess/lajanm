import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AuditModule } from '../audit/audit.module';
import { AuthModule } from '../auth/auth.module';
import { UploadedFile } from './entities/uploaded-file.entity';
import { UploadsController } from './uploads.controller';
import { UploadsService } from './uploads.service';

@Module({
  // AuthModule fournit le JwtService dont JwtAuthGuard a besoin.
  imports: [TypeOrmModule.forFeature([UploadedFile]), AuthModule, AuditModule],
  controllers: [UploadsController],
  providers: [UploadsService],
  exports: [UploadsService],
})
export class UploadsModule {}
