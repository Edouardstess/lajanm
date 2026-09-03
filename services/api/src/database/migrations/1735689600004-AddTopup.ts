import { MigrationInterface, QueryRunner } from 'typeorm';

export class AddTopup1735689600004 implements MigrationInterface {
  name = 'AddTopup1735689600004';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`CREATE TYPE "topup_source_enum" AS ENUM ('moncash', 'natcash')`);
    await queryRunner.query(
      `CREATE TYPE "topup_status_enum" AS ENUM ('pending', 'completed', 'failed')`,
    );
    await queryRunner.query(`
      CREATE TABLE "topup_transactions" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "userId" uuid NOT NULL,
        "source" topup_source_enum NOT NULL DEFAULT 'moncash',
        "amountMinor" bigint NOT NULL,
        "currency" varchar(3) NOT NULL DEFAULT 'HTG',
        "status" topup_status_enum NOT NULL DEFAULT 'pending',
        "providerReference" varchar(128) NULL,
        "operationId" uuid NULL,
        "failureReason" varchar(255) NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        "updatedAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_topup_transactions" PRIMARY KEY ("id"),
        CONSTRAINT "FK_topup_transactions_user" FOREIGN KEY ("userId")
          REFERENCES "users" ("id") ON DELETE RESTRICT,
        CONSTRAINT "FK_topup_transactions_operation" FOREIGN KEY ("operationId")
          REFERENCES "operations" ("id") ON DELETE RESTRICT
      )
    `);
    await queryRunner.query(
      `CREATE INDEX "IDX_topup_transactions_userId" ON "topup_transactions" ("userId")`,
    );
    // Partial unique index: providerReference is NULL until MonCash
    // assigns one, and plain UNIQUE would let multiple NULLs coexist
    // anyway, but being explicit documents the intent — this is the
    // idempotency anchor the webhook handler looks up by.
    await queryRunner.query(`
      CREATE UNIQUE INDEX "UQ_topup_transactions_providerReference"
      ON "topup_transactions" ("providerReference")
      WHERE "providerReference" IS NOT NULL
    `);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP TABLE "topup_transactions"`);
    await queryRunner.query(`DROP TYPE "topup_status_enum"`);
    await queryRunner.query(`DROP TYPE "topup_source_enum"`);
  }
}
