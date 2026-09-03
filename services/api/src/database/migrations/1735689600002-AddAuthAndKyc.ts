import { MigrationInterface, QueryRunner } from 'typeorm';

export class AddAuthAndKyc1735689600002 implements MigrationInterface {
  name = 'AddAuthAndKyc1735689600002';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`CREATE TYPE "user_tier_enum" AS ENUM ('basic', 'verified')`);
    await queryRunner.query(`
      CREATE TABLE "users" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "phone" varchar(32) NOT NULL,
        "pinHash" varchar(255) NOT NULL,
        "tier" user_tier_enum NOT NULL DEFAULT 'basic',
        "fullName" varchar(128) NULL,
        "email" varchar(128) NULL,
        "failedPinAttempts" int NOT NULL DEFAULT 0,
        "lockedUntil" timestamptz NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        "updatedAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_users" PRIMARY KEY ("id"),
        CONSTRAINT "UQ_users_phone" UNIQUE ("phone")
      )
    `);

    await queryRunner.query(`
      CREATE TABLE "device_sessions" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "userId" uuid NOT NULL,
        "deviceId" varchar(255) NOT NULL,
        "deviceName" varchar(128) NULL,
        "lastSeenAt" timestamptz NOT NULL,
        "revokedAt" timestamptz NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_device_sessions" PRIMARY KEY ("id"),
        CONSTRAINT "UQ_device_sessions_user_device" UNIQUE ("userId", "deviceId"),
        CONSTRAINT "FK_device_sessions_user" FOREIGN KEY ("userId")
          REFERENCES "users" ("id") ON DELETE CASCADE
      )
    `);
    await queryRunner.query(
      `CREATE INDEX "IDX_device_sessions_userId" ON "device_sessions" ("userId")`,
    );

    await queryRunner.query(`
      CREATE TYPE "kyc_status_enum" AS ENUM ('pending', 'approved', 'rejected')
    `);
    await queryRunner.query(`
      CREATE TABLE "kyc_submissions" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "userId" uuid NOT NULL,
        "idDocumentUrl" varchar(512) NOT NULL,
        "selfieUrl" varchar(512) NOT NULL,
        "status" kyc_status_enum NOT NULL DEFAULT 'pending',
        "reviewerId" uuid NULL,
        "reviewedAt" timestamptz NULL,
        "rejectionReason" varchar(255) NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_kyc_submissions" PRIMARY KEY ("id"),
        CONSTRAINT "FK_kyc_submissions_user" FOREIGN KEY ("userId")
          REFERENCES "users" ("id") ON DELETE CASCADE
      )
    `);
    await queryRunner.query(
      `CREATE INDEX "IDX_kyc_submissions_userId" ON "kyc_submissions" ("userId")`,
    );

    // accounts.ownerId references users.id for owner_type = 'user'; not a
    // formal FK because accounts also holds ownerId = NULL system rows.
    await queryRunner.query(
      `CREATE INDEX "IDX_accounts_ownerId_ownerType" ON "accounts" ("ownerId", "ownerType")`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP INDEX "IDX_accounts_ownerId_ownerType"`);
    await queryRunner.query(`DROP TABLE "kyc_submissions"`);
    await queryRunner.query(`DROP TYPE "kyc_status_enum"`);
    await queryRunner.query(`DROP TABLE "device_sessions"`);
    await queryRunner.query(`DROP TABLE "users"`);
    await queryRunner.query(`DROP TYPE "user_tier_enum"`);
  }
}
