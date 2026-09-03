import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { Account } from './entities/account.entity';
import { LedgerEntry } from './entities/ledger-entry.entity';
import { Operation } from './entities/operation.entity';
import { LedgerService } from './ledger.service';
import { AccountsService } from './services/accounts.service';

@Module({
  imports: [TypeOrmModule.forFeature([Account, Operation, LedgerEntry])],
  providers: [LedgerService, AccountsService],
  exports: [LedgerService, AccountsService],
})
export class LedgerModule {}
