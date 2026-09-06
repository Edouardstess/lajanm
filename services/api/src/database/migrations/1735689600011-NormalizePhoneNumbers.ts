import { MigrationInterface, QueryRunner } from 'typeorm';

/**
 * Ramène les numéros déjà stockés à la forme canonique E.164, celle que
 * `common/phone.ts` produit désormais à l'inscription.
 *
 * Sans cette migration, un compte créé avant le correctif reste
 * introuvable pour un expéditeur qui saisit le même numéro autrement, et
 * l'argent part vers un compte que le destinataire ne consulte pas.
 *
 * Les mêmes règles qu'en TypeScript, en SQL :
 *   8 chiffres              -> +509 + les 8 chiffres
 *   509 + 8 chiffres        -> + les 11 chiffres
 *   00 + 8 à 15 chiffres    -> + les chiffres restants
 *   déjà en +…              -> chiffres uniquement, préfixés de +
 *
 * Le cas dangereux est traité en premier : si deux comptes distincts se
 * ramènent au MÊME numéro canonique, la migration s'arrête. Fusionner
 * deux comptes qui détiennent chacun un solde n'est pas une décision que
 * peut prendre un script — il faut décider lequel garde l'argent.
 */
export class NormalizePhoneNumbers1735689600011 implements MigrationInterface {
  name = 'NormalizePhoneNumbers1735689600011';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`
      CREATE OR REPLACE FUNCTION pg_temp.lajanm_normalize_phone(raw text)
      RETURNS text AS $$
      DECLARE
        digits text := regexp_replace(coalesce(raw, ''), '\\D', '', 'g');
      BEGIN
        IF digits = '' THEN
          RETURN raw;
        END IF;
        IF left(trim(coalesce(raw, '')), 1) = '+' THEN
          RETURN '+' || digits;
        END IF;
        -- Le national avant l'international, pour la même raison qu'en
        -- TypeScript : « 00 » en tête de 8 chiffres n'est pas un préfixe
        -- d'appel international, c'est le début du numéro.
        IF length(digits) = 8 THEN
          RETURN '+509' || digits;
        END IF;
        IF left(digits, 2) = '00' THEN
          RETURN '+' || substr(digits, 3);
        END IF;
        IF left(digits, 3) = '509' AND length(digits) = 11 THEN
          RETURN '+' || digits;
        END IF;
        -- Format non reconnu : laissé tel quel plutôt que deviné.
        RETURN raw;
      END;
      $$ LANGUAGE plpgsql IMMUTABLE;
    `);

    const collisions: Array<{ canonical: string; phones: string[] }> = await queryRunner.query(`
      SELECT pg_temp.lajanm_normalize_phone("phone") AS canonical,
             array_agg("phone" ORDER BY "phone") AS phones
        FROM "users"
       GROUP BY 1
      HAVING count(*) > 1
    `);

    if (collisions.length > 0) {
      const detail = collisions
        .map((c) => `${c.canonical} <- ${c.phones.join(', ')}`)
        .join(' | ');
      throw new Error(
        'Normalisation des numéros impossible : plusieurs comptes se ramènent au même numéro. ' +
          'Ces comptes doivent être fusionnés à la main (décider lequel conserve le solde) ' +
          `avant de rejouer la migration. Collisions : ${detail}`,
      );
    }

    await queryRunner.query(`
      UPDATE "users"
         SET "phone" = pg_temp.lajanm_normalize_phone("phone")
       WHERE "phone" <> pg_temp.lajanm_normalize_phone("phone")
    `);
  }

  /**
   * Irréversible par nature : la forme d'origine de chaque numéro n'est
   * conservée nulle part. Le `down` ne prétend donc rien défaire — il
   * réussit sans agir, la forme canonique restant valide pour le code
   * antérieur, qui comparait des chaînes brutes.
   */
  public async down(): Promise<void> {
    // Rien à défaire : voir le commentaire ci-dessus.
  }
}
