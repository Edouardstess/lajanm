import { MigrationInterface, QueryRunner } from 'typeorm';

export class AddPayout1735689600005 implements MigrationInterface {
  name = 'AddPayout1735689600005';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`CREATE TYPE "payout_status_enum" AS ENUM ('completed', 'failed')`);
    await queryRunner.query(`
      CREATE TABLE "payout_transactions" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "userId" uuid NOT NULL,
        "amountMinor" bigint NOT NULL,
        "currency" varchar(3) NOT NULL DEFAULT 'HTG',
        "status" payout_status_enum NOT NULL,
        "operationId" uuid NOT NULL,
        "reversalOperationId" uuid NULL,
        "providerReference" varchar(128) NULL,
        "failureReason" varchar(255) NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        "updatedAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_payout_transactions" PRIMARY KEY ("id"),
        CONSTRAINT "FK_payout_transactions_user" FOREIGN KEY ("userId")
          REFERENCES "users" ("id") ON DELETE RESTRICT,
        CONSTRAINT "FK_payout_transactions_operation" FOREIGN KEY ("operationId")
          REFERENCES "operations" ("id") ON DELETE RESTRICT,
        CONSTRAINT "FK_payout_transactions_reversal_operation" FOREIGN KEY ("reversalOperationId")
          REFERENCES "operations" ("id") ON DELETE RESTRICT
      )
    `);
    await queryRunner.query(
      `CREATE INDEX "IDX_payout_transactions_userId" ON "payout_transactions" ("userId")`,
    );
    await queryRunner.query(
      `CREATE UNIQUE INDEX "UQ_payout_transactions_operationId" ON "payout_transactions" ("operationId")`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP TABLE "payout_transactions"`);
    await queryRunner.query(`DROP TYPE "payout_status_enum"`);
  }
}
