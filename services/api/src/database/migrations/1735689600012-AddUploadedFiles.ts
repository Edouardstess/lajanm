import { MigrationInterface, QueryRunner } from 'typeorm';

/**
 * Table des fichiers déposés, et bascule du KYC vers des identifiants de
 * fichiers plutôt que des chaînes libres.
 *
 * Avant : `kyc_submissions.idDocumentUrl` acceptait n'importe quelle
 * chaîne non vide — l'application mobile y écrivait l'URI local du
 * téléphone, qui ne désigne rien côté serveur. Aucun fichier n'était donc
 * réellement transmis, et rien n'était vérifié.
 */
export class AddUploadedFiles1735689600012 implements MigrationInterface {
  name = 'AddUploadedFiles1735689600012';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`
      CREATE TABLE "uploaded_files" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "ownerId" uuid NOT NULL,
        "format" varchar(16) NOT NULL,
        "sizeBytes" integer NOT NULL,
        "sha256" varchar(64) NOT NULL,
        "storedName" varchar(128) NOT NULL,
        "createdAt" timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT "PK_uploaded_files" PRIMARY KEY ("id"),
        CONSTRAINT "UQ_uploaded_files_stored_name" UNIQUE ("storedName"),
        CONSTRAINT "FK_uploaded_files_owner" FOREIGN KEY ("ownerId")
          REFERENCES "users" ("id") ON DELETE RESTRICT
      )
    `);
    await queryRunner.query(`CREATE INDEX "IDX_uploaded_files_owner" ON "uploaded_files" ("ownerId")`);

    // Les demandes en cours pointent sur des URI locaux de téléphone, qui
    // ne désignent aucun fichier accessible. Les rattacher à un fichier
    // inexistant serait pire que de les refuser : la colonne devient
    // nullable, et les anciennes valeurs sont effacées.
    await queryRunner.query(`
      ALTER TABLE "kyc_submissions"
        ADD COLUMN "idDocumentFileId" uuid NULL,
        ADD COLUMN "selfieFileId" uuid NULL,
        ADD CONSTRAINT "FK_kyc_id_document_file" FOREIGN KEY ("idDocumentFileId")
          REFERENCES "uploaded_files" ("id") ON DELETE RESTRICT,
        ADD CONSTRAINT "FK_kyc_selfie_file" FOREIGN KEY ("selfieFileId")
          REFERENCES "uploaded_files" ("id") ON DELETE RESTRICT
    `);
    await queryRunner.query(`ALTER TABLE "kyc_submissions" DROP COLUMN "idDocumentUrl"`);
    await queryRunner.query(`ALTER TABLE "kyc_submissions" DROP COLUMN "selfieUrl"`);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`
      ALTER TABLE "kyc_submissions"
        ADD COLUMN "idDocumentUrl" varchar(512) NOT NULL DEFAULT '',
        ADD COLUMN "selfieUrl" varchar(512) NOT NULL DEFAULT ''
    `);
    await queryRunner.query(`
      ALTER TABLE "kyc_submissions"
        DROP CONSTRAINT "FK_kyc_id_document_file",
        DROP CONSTRAINT "FK_kyc_selfie_file",
        DROP COLUMN "idDocumentFileId",
        DROP COLUMN "selfieFileId"
    `);
    await queryRunner.query(`DROP INDEX "IDX_uploaded_files_owner"`);
    await queryRunner.query(`DROP TABLE "uploaded_files"`);
  }
}
