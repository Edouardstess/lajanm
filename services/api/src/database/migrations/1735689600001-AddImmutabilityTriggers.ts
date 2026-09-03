import { MigrationInterface, QueryRunner } from 'typeorm';

/**
 * Enforces append-only semantics at the database level for the two tables
 * where it matters most: audit_logs (NF-06) and ledger_entries (the
 * ledger's individual movements must never be edited or removed — a
 * mistake is corrected with a new reversing entry, see LedgerService.
 * reverseOperation). This is a second line of defense: even if application
 * code has a bug, or a future engineer forgets, or a database admin runs an
 * ad-hoc UPDATE, Postgres itself refuses the statement.
 *
 * operations and accounts are intentionally NOT covered — an Operation's
 * status legitimately transitions (pending -> completed/reversed/failed),
 * and accounts may gain descriptive metadata over time.
 */
export class AddImmutabilityTriggers1735689600001 implements MigrationInterface {
  name = 'AddImmutabilityTriggers1735689600001';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`
      CREATE OR REPLACE FUNCTION reject_mutation() RETURNS trigger AS $$
      BEGIN
        RAISE EXCEPTION '% on % is not allowed: this table is append-only', TG_OP, TG_TABLE_NAME;
        RETURN NULL;
      END;
      $$ LANGUAGE plpgsql;
    `);

    for (const table of ['audit_logs', 'ledger_entries']) {
      await queryRunner.query(`
        CREATE TRIGGER "trg_${table}_reject_update"
        BEFORE UPDATE ON "${table}"
        FOR EACH ROW EXECUTE FUNCTION reject_mutation();
      `);
      await queryRunner.query(`
        CREATE TRIGGER "trg_${table}_reject_delete"
        BEFORE DELETE ON "${table}"
        FOR EACH ROW EXECUTE FUNCTION reject_mutation();
      `);
    }
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    for (const table of ['audit_logs', 'ledger_entries']) {
      await queryRunner.query(`DROP TRIGGER IF EXISTS "trg_${table}_reject_update" ON "${table}"`);
      await queryRunner.query(`DROP TRIGGER IF EXISTS "trg_${table}_reject_delete" ON "${table}"`);
    }
    await queryRunner.query(`DROP FUNCTION IF EXISTS reject_mutation()`);
  }
}
