import { MigrationInterface, QueryRunner } from 'typeorm';

export class AddFraudFlags1735689600007 implements MigrationInterface {
  name = 'AddFraudFlags1735689600007';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`
      CREATE TYPE "fraud_flag_status_enum" AS ENUM ('open', 'resolved', 'confirmed_suspect', 'false_positive')
    `);
    await queryRunner.query(`
      CREATE TYPE "fraud_rule_code_enum" AS ENUM ('high_velocity', 'amount_anomaly', 'frequent_new_beneficiaries')
    `);
    await queryRunner.query(`
      CREATE TABLE "fraud_flags" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "userId" uuid NOT NULL,
        "ruleCode" fraud_rule_code_enum NOT NULL,
        "status" fraud_flag_status_enum NOT NULL DEFAULT 'open',
        "relatedOperationId" uuid NULL,
        "details" jsonb NULL,
        "resolvedBy" uuid NULL,
        "resolvedAt" timestamptz NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_fraud_flags" PRIMARY KEY ("id")
      )
    `);
    await queryRunner.query(`CREATE INDEX "IDX_fraud_flags_userId" ON "fraud_flags" ("userId")`);
    await queryRunner.query(`CREATE INDEX "IDX_fraud_flags_status" ON "fraud_flags" ("status")`);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP TABLE "fraud_flags"`);
    await queryRunner.query(`DROP TYPE "fraud_rule_code_enum"`);
    await queryRunner.query(`DROP TYPE "fraud_flag_status_enum"`);
  }
}
