import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AdminModule } from '../admin/admin.module';
import { AuditModule } from '../audit/audit.module';
import { AuthModule } from '../auth/auth.module';
import { FaqEntry } from './entities/faq-entry.entity';
import { SupportMessage } from './entities/support-message.entity';
import { SupportTicket } from './entities/support-ticket.entity';
import { SupportController } from './support.controller';
import { SupportService } from './support.service';

@Module({
  imports: [
    TypeOrmModule.forFeature([SupportTicket, SupportMessage, FaqEntry]),
    AuthModule,
    AdminModule,
    AuditModule,
  ],
  controllers: [SupportController],
  providers: [SupportService],
  exports: [SupportService],
})
export class SupportModule {}
