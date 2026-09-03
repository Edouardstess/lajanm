import { MigrationInterface, QueryRunner } from 'typeorm';

export class AddCompliance1735689600009 implements MigrationInterface {
  name = 'AddCompliance1735689600009';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`
      CREATE TYPE "dispute_status_enum" AS ENUM ('open', 'investigating', 'resolved', 'rejected')
    `);
    await queryRunner.query(`
      CREATE TABLE "disputes" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "userId" uuid NOT NULL,
        "relatedOperationId" uuid NULL,
        "subject" varchar(255) NOT NULL,
        "description" text NOT NULL,
        "status" dispute_status_enum NOT NULL DEFAULT 'open',
        "internalNotes" text NULL,
        "assignedTo" uuid NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        "updatedAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_disputes" PRIMARY KEY ("id"),
        CONSTRAINT "FK_disputes_user" FOREIGN KEY ("userId")
          REFERENCES "users" ("id") ON DELETE RESTRICT
      )
    `);
    await queryRunner.query(`CREATE INDEX "IDX_disputes_userId" ON "disputes" ("userId")`);

    await queryRunner.query(`
      CREATE TABLE "suspicious_activity_reports" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "subjectUserId" uuid NOT NULL,
        "relatedOperationIds" jsonb NOT NULL,
        "reason" text NOT NULL,
        "filedBy" uuid NOT NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_suspicious_activity_reports" PRIMARY KEY ("id"),
        CONSTRAINT "FK_sar_subject_user" FOREIGN KEY ("subjectUserId")
          REFERENCES "users" ("id") ON DELETE RESTRICT
      )
    `);
    await queryRunner.query(
      `CREATE INDEX "IDX_sar_subjectUserId" ON "suspicious_activity_reports" ("subjectUserId")`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP TABLE "suspicious_activity_reports"`);
    await queryRunner.query(`DROP TABLE "disputes"`);
    await queryRunner.query(`DROP TYPE "dispute_status_enum"`);
  }
}
