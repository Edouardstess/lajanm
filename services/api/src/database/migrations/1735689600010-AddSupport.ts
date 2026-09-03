import { MigrationInterface, QueryRunner } from 'typeorm';

export class AddSupport1735689600010 implements MigrationInterface {
  name = 'AddSupport1735689600010';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`
      CREATE TYPE "ticket_category_enum" AS ENUM ('general', 'transaction', 'kyc', 'technical', 'other')
    `);
    await queryRunner.query(`
      CREATE TYPE "ticket_status_enum" AS ENUM ('open', 'in_progress', 'resolved', 'closed')
    `);
    await queryRunner.query(`
      CREATE TABLE "support_tickets" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "userId" uuid NOT NULL,
        "subject" varchar(255) NOT NULL,
        "category" ticket_category_enum NOT NULL DEFAULT 'general',
        "status" ticket_status_enum NOT NULL DEFAULT 'open',
        "assignedTo" uuid NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        "updatedAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_support_tickets" PRIMARY KEY ("id"),
        CONSTRAINT "FK_support_tickets_user" FOREIGN KEY ("userId")
          REFERENCES "users" ("id") ON DELETE RESTRICT
      )
    `);
    await queryRunner.query(`CREATE INDEX "IDX_support_tickets_userId" ON "support_tickets" ("userId")`);
    await queryRunner.query(`CREATE INDEX "IDX_support_tickets_status" ON "support_tickets" ("status")`);

    await queryRunner.query(`
      CREATE TYPE "support_sender_type_enum" AS ENUM ('user', 'admin')
    `);
    await queryRunner.query(`
      CREATE TABLE "support_messages" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "ticketId" uuid NOT NULL,
        "senderId" uuid NOT NULL,
        "senderType" support_sender_type_enum NOT NULL,
        "body" text NOT NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_support_messages" PRIMARY KEY ("id"),
        CONSTRAINT "FK_support_messages_ticket" FOREIGN KEY ("ticketId")
          REFERENCES "support_tickets" ("id") ON DELETE CASCADE
      )
    `);
    await queryRunner.query(
      `CREATE INDEX "IDX_support_messages_ticketId" ON "support_messages" ("ticketId")`,
    );

    // No FK on "senderId": it points at users.id for a customer message and
    // admin_users.id for an operator reply — two different tables, so the
    // column is a plain uuid discriminated by "senderType".
    await queryRunner.query(`
      CREATE TABLE "faq_entries" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "category" varchar(100) NOT NULL,
        "question" varchar(500) NOT NULL,
        "answer" text NOT NULL,
        "sortOrder" integer NOT NULL DEFAULT 0,
        "isPublished" boolean NOT NULL DEFAULT true,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        "updatedAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_faq_entries" PRIMARY KEY ("id")
      )
    `);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP TABLE "faq_entries"`);
    await queryRunner.query(`DROP TABLE "support_messages"`);
    await queryRunner.query(`DROP TYPE "support_sender_type_enum"`);
    await queryRunner.query(`DROP TABLE "support_tickets"`);
    await queryRunner.query(`DROP TYPE "ticket_status_enum"`);
    await queryRunner.query(`DROP TYPE "ticket_category_enum"`);
  }
}
