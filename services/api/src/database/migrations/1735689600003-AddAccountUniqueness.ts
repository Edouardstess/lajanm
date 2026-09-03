import { MigrationInterface, QueryRunner } from 'typeorm';

/**
 * Prevents two concurrent "get or create wallet account" calls (see
 * AccountsService) from creating duplicate accounts for the same owner —
 * a duplicate wallet account would silently split a user's balance across
 * two rows. Partial indexes because uniqueness means something different
 * per owner type: exactly one row per (user, account name) for user
 * accounts, and exactly one row per name for system accounts (ownerId is
 * NULL there, and plain UNIQUE treats NULLs as distinct, so a partial
 * index is required rather than a table-wide UNIQUE constraint).
 */
export class AddAccountUniqueness1735689600003 implements MigrationInterface {
  name = 'AddAccountUniqueness1735689600003';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`
      CREATE UNIQUE INDEX "UQ_accounts_user_owner_name"
      ON "accounts" ("ownerId", "name")
      WHERE "ownerType" = 'user'
    `);
    await queryRunner.query(`
      CREATE UNIQUE INDEX "UQ_accounts_system_name"
      ON "accounts" ("name")
      WHERE "ownerType" = 'system'
    `);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP INDEX "UQ_accounts_system_name"`);
    await queryRunner.query(`DROP INDEX "UQ_accounts_user_owner_name"`);
  }
}
