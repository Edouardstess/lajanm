import { MigrationInterface, QueryRunner } from 'typeorm';

export class AddSecurityOtp1735689600006 implements MigrationInterface {
  name = 'AddSecurityOtp1735689600006';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`CREATE TYPE "otp_purpose_enum" AS ENUM ('transfer', 'payout')`);
    await queryRunner.query(`
      CREATE TABLE "otp_codes" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "userId" uuid NOT NULL,
        "purpose" otp_purpose_enum NOT NULL,
        "codeHash" varchar(64) NOT NULL,
        "attempts" int NOT NULL DEFAULT 0,
        "maxAttempts" int NOT NULL DEFAULT 3,
        "expiresAt" timestamptz NOT NULL,
        "consumedAt" timestamptz NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_otp_codes" PRIMARY KEY ("id"),
        CONSTRAINT "FK_otp_codes_user" FOREIGN KEY ("userId")
          REFERENCES "users" ("id") ON DELETE CASCADE
      )
    `);
    await queryRunner.query(`CREATE INDEX "IDX_otp_codes_userId" ON "otp_codes" ("userId")`);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP TABLE "otp_codes"`);
    await queryRunner.query(`DROP TYPE "otp_purpose_enum"`);
  }
}
