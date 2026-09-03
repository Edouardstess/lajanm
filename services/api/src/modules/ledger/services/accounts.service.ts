import { Injectable } from '@nestjs/common';
import { InjectDataSource } from '@nestjs/typeorm';
import { DataSource, IsNull, QueryFailedError } from 'typeorm';
import { Account, AccountOwnerType } from '../entities/account.entity';

const POSTGRES_UNIQUE_VIOLATION = '23505';

/**
 * Get-or-create for ledger accounts. Every module that needs to post
 * ledger operations (topup, payout, wallet) resolves its accounts through
 * here rather than querying/inserting `accounts` directly, so the
 * uniqueness guarantee (one wallet per user, one row per named system
 * account — see AddAccountUniqueness migration) is enforced in exactly
 * one place.
 */
@Injectable()
export class AccountsService {
  constructor(@InjectDataSource() private readonly dataSource: DataSource) {}

  async getOrCreateUserWalletAccount(userId: string, currency = 'HTG'): Promise<Account> {
    return this.getOrCreate({ ownerType: AccountOwnerType.USER, ownerId: userId, name: 'wallet', currency });
  }

  /** Named system accounts, e.g. 'moncash_float', 'natcash_float', 'fees'. */
  async getOrCreateSystemAccount(name: string, currency = 'HTG'): Promise<Account> {
    return this.getOrCreate({ ownerType: AccountOwnerType.SYSTEM, ownerId: null, name, currency });
  }

  private async getOrCreate(spec: {
    ownerType: AccountOwnerType;
    ownerId: string | null;
    name: string;
    currency: string;
  }): Promise<Account> {
    const repo = this.dataSource.getRepository(Account);
    const whereClause = {
      ownerType: spec.ownerType,
      ownerId: spec.ownerId === null ? IsNull() : spec.ownerId,
      name: spec.name,
    };
    const existing = await repo.findOneBy(whereClause);
    if (existing) return existing;

    try {
      return await repo.save(repo.create(spec));
    } catch (error) {
      if (
        error instanceof QueryFailedError &&
        (error as unknown as { code?: string }).code === POSTGRES_UNIQUE_VIOLATION
      ) {
        // Lost the race to a concurrent getOrCreate — fetch what the
        // other call just committed instead of erroring.
        return repo.findOneByOrFail(whereClause);
      }
      throw error;
    }
  }
}
