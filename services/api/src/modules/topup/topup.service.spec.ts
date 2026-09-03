import { UnauthorizedException } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { createHmac, randomUUID } from 'crypto';
import { DataSource, QueryFailedError } from 'typeorm';
import { AuditService } from '../audit/audit.service';
import { Account, AccountOwnerType } from '../ledger/entities/account.entity';
import { Operation } from '../ledger/entities/operation.entity';
import { LedgerService } from '../ledger/ledger.service';
import { AccountsService } from '../ledger/services/accounts.service';
import { TopupStatus, TopupTransaction } from './entities/topup-transaction.entity';
import { MonCashClient, MonCashUnavailableError } from './moncash-client.service';
import { TopupService } from './topup.service';

const WEBHOOK_SECRET = 'test-webhook-secret';

function sign(body: string): string {
  return createHmac('sha256', WEBHOOK_SECRET).update(body).digest('hex');
}

/**
 * Same shape as the FakeDataSource in ledger.service.spec.ts: transaction()
 * rolls back only what it itself inserted, and a duplicate idempotencyKey
 * throws a real QueryFailedError with code 23505. Reused here (rather than
 * mocking LedgerService outright) so this test proves the actual
 * idempotency guarantee the webhook handler depends on, not a stub.
 */
class FakeLedgerDataSource {
  operations: Array<Record<string, unknown>> = [];
  entries: Array<Record<string, unknown>> = [];

  async transaction<T>(cb: (manager: unknown) => Promise<T>): Promise<T> {
    const insertedOperations: Array<Record<string, unknown>> = [];
    const insertedEntries: Array<Record<string, unknown>> = [];
    try {
      return await cb(this.manager(insertedOperations, insertedEntries));
    } catch (error) {
      this.operations = this.operations.filter((o) => !insertedOperations.includes(o));
      this.entries = this.entries.filter((e) => !insertedEntries.includes(e));
      throw error;
    }
  }

  getRepository(entityClass: unknown) {
    return this.manager([], []).getRepository(entityClass);
  }

  private manager(
    insertedOperations: Array<Record<string, unknown>>,
    insertedEntries: Array<Record<string, unknown>>,
  ) {
    return {
      getRepository: (entityClass: unknown) => {
        if (entityClass === Operation) {
          return {
            create: (data: Record<string, unknown>) => ({ id: randomUUID(), ...data }),
            save: async (data: Record<string, unknown>) => {
              if (this.operations.some((o) => o.idempotencyKey === data.idempotencyKey)) {
                const driverError = Object.assign(new Error('duplicate key'), { code: '23505' });
                throw new QueryFailedError('INSERT INTO operations ...', [], driverError);
              }
              this.operations.push(data);
              insertedOperations.push(data);
              return data;
            },
            findOneByOrFail: async (where: Record<string, unknown>) => {
              const found = this.operations.find((o) => o.idempotencyKey === where.idempotencyKey);
              if (!found) throw new Error('not found');
              return found;
            },
          };
        }
        return {
          create: (data: Record<string, unknown>) => ({ id: randomUUID(), ...data }),
          save: async (data: Array<Record<string, unknown>>) => {
            this.entries.push(...data);
            insertedEntries.push(...data);
            return data;
          },
          findBy: async (where: Record<string, unknown>) =>
            this.entries.filter((e) => e.operationId === where.operationId),
        };
      },
    };
  }
}

function createTransactionsRepo() {
  const rows: TopupTransaction[] = [];
  return {
    rows,
    create: jest.fn(
      (data: Partial<TopupTransaction>) =>
        ({
          id: randomUUID(),
          operationId: null,
          providerReference: null,
          failureReason: null,
          status: TopupStatus.PENDING,
          currency: 'HTG',
          createdAt: new Date(),
          updatedAt: new Date(),
          ...data,
        }) as TopupTransaction,
    ),
    save: jest.fn(async (entity: TopupTransaction) => {
      const i = rows.findIndex((r) => r.id === entity.id);
      if (i === -1) rows.push(entity);
      else rows[i] = entity;
      return entity;
    }),
    findOneBy: jest.fn(async (where: Partial<TopupTransaction>) => {
      const keys = Object.keys(where) as Array<keyof TopupTransaction>;
      return rows.find((r) => keys.every((k) => r[k] === where[k])) ?? null;
    }),
  };
}

const walletAccount: Account = {
  id: 'wallet-acc',
  ownerType: AccountOwnerType.USER,
  ownerId: 'user-1',
  name: 'wallet',
  currency: 'HTG',
  createdAt: new Date(),
};
const floatAccount: Account = {
  id: 'float-acc',
  ownerType: AccountOwnerType.SYSTEM,
  ownerId: null,
  name: 'moncash_float',
  currency: 'HTG',
  createdAt: new Date(),
};

describe('TopupService webhook handling', () => {
  let transactions: ReturnType<typeof createTransactionsRepo>;
  let fakeLedgerDataSource: FakeLedgerDataSource;
  let service: TopupService;

  beforeEach(() => {
    transactions = createTransactionsRepo();
    fakeLedgerDataSource = new FakeLedgerDataSource();

    const config = new ConfigService({ MONCASH_WEBHOOK_SECRET: WEBHOOK_SECRET });
    const monCashClient = new MonCashClient(config);
    const ledgerService = new LedgerService(fakeLedgerDataSource as unknown as DataSource);
    const accountsService = {
      getOrCreateUserWalletAccount: jest.fn().mockResolvedValue(walletAccount),
      getOrCreateSystemAccount: jest.fn().mockResolvedValue(floatAccount),
    } as unknown as AccountsService;
    const auditService = { record: jest.fn().mockResolvedValue(undefined) } as unknown as AuditService;

    service = new TopupService(
      transactions as unknown as never,
      { add: jest.fn() } as unknown as never,
      monCashClient,
      ledgerService,
      accountsService,
      auditService,
    );
  });

  it('rejects a webhook with an invalid signature and credits nothing', async () => {
    const body = JSON.stringify({ reference: 'ref-1', status: 'completed' });
    await expect(service.handleWebhook(body, 'not-the-right-signature')).rejects.toThrow(
      UnauthorizedException,
    );
    expect(fakeLedgerDataSource.entries).toHaveLength(0);
  });

  it('credits the ledger exactly once even when the webhook is redelivered', async () => {
    const transaction = transactions.create({
      userId: 'user-1',
      amountMinor: '50000',
      providerReference: 'moncash-ref-1',
    });
    await transactions.save(transaction);

    const body = JSON.stringify({ reference: 'moncash-ref-1', status: 'completed' });
    const signature = sign(body);

    await service.handleWebhook(body, signature);
    await service.handleWebhook(body, signature); // simulates MonCash redelivering the webhook

    expect(fakeLedgerDataSource.entries).toHaveLength(2); // one credit + one debit, not four
    const saved = await transactions.findOneBy({ id: transaction.id });
    expect(saved?.status).toBe(TopupStatus.COMPLETED);
    expect(saved?.operationId).toBeTruthy();
  });

  it('ignores a webhook for a reference with no matching transaction', async () => {
    const body = JSON.stringify({ reference: 'unknown-ref', status: 'completed' });
    await service.handleWebhook(body, sign(body));
    expect(fakeLedgerDataSource.entries).toHaveLength(0);
  });
});

describe('TopupService.initiate', () => {
  it('queues a retry and keeps the transaction honestly pending when MonCash is unavailable', async () => {
    const transactions = createTransactionsRepo();
    const retryQueue = { add: jest.fn() };
    const monCashClient = {
      createPayment: jest.fn().mockRejectedValue(new MonCashUnavailableError('down')),
    } as unknown as MonCashClient;

    const service = new TopupService(
      transactions as unknown as never,
      retryQueue as unknown as never,
      monCashClient,
      {} as LedgerService,
      {} as AccountsService,
      { record: jest.fn().mockResolvedValue(undefined) } as unknown as AuditService,
    );

    const result = await service.initiate('user-1', { amountHTG: 100 });

    expect(result.status).toBe(TopupStatus.PENDING);
    expect(result.gatewayUrl).toBeNull();
    expect(retryQueue.add).toHaveBeenCalledTimes(1);
  });
});
