import { MigrationInterface, QueryRunner } from 'typeorm';

export class InitSchema1735689600000 implements MigrationInterface {
  name = 'InitSchema1735689600000';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`CREATE EXTENSION IF NOT EXISTS "pgcrypto"`);

    await queryRunner.query(`
      CREATE TYPE "account_owner_type_enum" AS ENUM ('user', 'system')
    `);
    await queryRunner.query(`
      CREATE TABLE "accounts" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "ownerType" account_owner_type_enum NOT NULL,
        "ownerId" uuid NULL,
        "name" varchar(64) NOT NULL,
        "currency" varchar(3) NOT NULL DEFAULT 'HTG',
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_accounts" PRIMARY KEY ("id")
      )
    `);
    await queryRunner.query(`CREATE INDEX "IDX_accounts_ownerId" ON "accounts" ("ownerId")`);

    await queryRunner.query(`
      CREATE TYPE "operation_type_enum" AS ENUM ('topup', 'payout', 'transfer', 'adjustment')
    `);
    await queryRunner.query(`
      CREATE TYPE "operation_status_enum" AS ENUM ('pending', 'completed', 'reversed', 'failed')
    `);
    await queryRunner.query(`
      CREATE TABLE "operations" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "idempotencyKey" varchar(255) NOT NULL,
        "type" operation_type_enum NOT NULL,
        "status" operation_status_enum NOT NULL DEFAULT 'completed',
        "metadata" jsonb NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        "updatedAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_operations" PRIMARY KEY ("id"),
        -- The idempotency guarantee (NF-02): a retried request with the same
        -- key can never create a second operation.
        CONSTRAINT "UQ_operations_idempotencyKey" UNIQUE ("idempotencyKey")
      )
    `);

    await queryRunner.query(`
      CREATE TYPE "entry_direction_enum" AS ENUM ('debit', 'credit')
    `);
    await queryRunner.query(`
      CREATE TABLE "ledger_entries" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "operationId" uuid NOT NULL,
        "accountId" uuid NOT NULL,
        "direction" entry_direction_enum NOT NULL,
        "amountMinor" bigint NOT NULL,
        "currency" varchar(3) NOT NULL DEFAULT 'HTG',
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_ledger_entries" PRIMARY KEY ("id"),
        CONSTRAINT "CHK_ledger_entries_amount_positive" CHECK ("amountMinor" > 0),
        CONSTRAINT "FK_ledger_entries_operation" FOREIGN KEY ("operationId")
          REFERENCES "operations" ("id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ledger_entries_account" FOREIGN KEY ("accountId")
          REFERENCES "accounts" ("id") ON DELETE RESTRICT
      )
    `);
    await queryRunner.query(
      `CREATE INDEX "IDX_ledger_entries_operationId" ON "ledger_entries" ("operationId")`,
    );
    await queryRunner.query(
      `CREATE INDEX "IDX_ledger_entries_accountId" ON "ledger_entries" ("accountId")`,
    );
    await queryRunner.query(
      `CREATE INDEX "IDX_ledger_entries_accountId_createdAt" ON "ledger_entries" ("accountId", "createdAt")`,
    );

    await queryRunner.query(`
      CREATE TABLE "audit_logs" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "action" varchar(128) NOT NULL,
        "actorId" varchar(128) NOT NULL,
        "actorType" varchar(32) NOT NULL,
        "targetId" varchar(128) NULL,
        "metadata" jsonb NULL,
        "ipAddress" varchar(64) NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_audit_logs" PRIMARY KEY ("id")
      )
    `);
    await queryRunner.query(`CREATE INDEX "IDX_audit_logs_action" ON "audit_logs" ("action")`);
    await queryRunner.query(
      `CREATE INDEX "IDX_audit_logs_actorId_createdAt" ON "audit_logs" ("actorId", "createdAt")`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_audit_logs_targetId" ON "audit_logs" ("targetId")`);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP TABLE "audit_logs"`);
    await queryRunner.query(`DROP TABLE "ledger_entries"`);
    await queryRunner.query(`DROP TYPE "entry_direction_enum"`);
    await queryRunner.query(`DROP TABLE "operations"`);
    await queryRunner.query(`DROP TYPE "operation_status_enum"`);
    await queryRunner.query(`DROP TYPE "operation_type_enum"`);
    await queryRunner.query(`DROP TABLE "accounts"`);
    await queryRunner.query(`DROP TYPE "account_owner_type_enum"`);
  }
}
