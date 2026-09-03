import { MigrationInterface, QueryRunner } from 'typeorm';

export class AddAdminUsers1735689600008 implements MigrationInterface {
  name = 'AddAdminUsers1735689600008';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`CREATE TYPE "admin_role_enum" AS ENUM ('operator', 'admin')`);
    await queryRunner.query(`
      CREATE TABLE "admin_users" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "email" varchar(255) NOT NULL,
        "passwordHash" varchar(255) NOT NULL,
        "role" admin_role_enum NOT NULL DEFAULT 'operator',
        "fullName" varchar(128) NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_admin_users" PRIMARY KEY ("id"),
        CONSTRAINT "UQ_admin_users_email" UNIQUE ("email")
      )
    `);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP TABLE "admin_users"`);
    await queryRunner.query(`DROP TYPE "admin_role_enum"`);
  }
}
