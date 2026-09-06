import { BadRequestException } from '@nestjs/common';

/**
 * Met un numéro de téléphone sous forme canonique E.164.
 *
 * Pourquoi ce fichier existe : les numéros étaient stockés et comparés
 * tels que saisis. Un compte créé avec « +50939000001 » était donc
 * introuvable pour qui tapait « 39000001 » ou « 50939000001 », et rien
 * n'empêchait deux comptes de coexister pour la même personne — dont un
 * seul recevrait l'argent. Pour un portefeuille, c'est un défaut qui
 * coûte de l'argent réel, pas une coquetterie de format.
 *
 * Règles, dans l'ordre :
 *   +509 39 00 00 01  ->  +50939000001   (séparateurs ignorés)
 *   0050939000001     ->  +50939000001   (préfixe international 00)
 *   50939000001       ->  +50939000001   (indicatif haïtien sans +)
 *   39000001          ->  +50939000001   (numéro national à 8 chiffres)
 *   +33612345678      ->  +33612345678   (étranger : conservé tel quel)
 *
 * Tout autre numéro sans « + » est refusé plutôt que deviné : préfixer
 * au hasard un numéro étranger de 9 ou 10 chiffres avec 509 fabriquerait
 * un destinataire qui n'existe pas.
 */
const HAITI_COUNTRY_CODE = '509';
const HAITI_NATIONAL_LENGTH = 8;

export function normalizePhone(input: string): string {
  const trimmed = (input ?? '').trim();
  const hasPlus = trimmed.startsWith('+');
  const digits = trimmed.replace(/\D/g, '');

  if (digits.length === 0) {
    throw new BadRequestException('Phone number is required');
  }

  if (hasPlus) {
    assertInternationalLength(digits);
    return `+${digits}`;
  }

  // Le cas national est traité AVANT le préfixe international : sinon un
  // numéro haïtien de 8 chiffres commençant par 00 est amputé de ses deux
  // premiers chiffres et devient un numéro qui n'existe pas. « 00 » ne
  // peut désigner un appel international que s'il reste un indicatif de
  // pays derrière, donc si le numéro est plus long que 8 chiffres.
  if (digits.length === HAITI_NATIONAL_LENGTH) {
    return `+${HAITI_COUNTRY_CODE}${digits}`;
  }

  if (digits.startsWith('00')) {
    const withoutPrefix = digits.slice(2);
    assertInternationalLength(withoutPrefix);
    return `+${withoutPrefix}`;
  }

  if (
    digits.startsWith(HAITI_COUNTRY_CODE) &&
    digits.length === HAITI_COUNTRY_CODE.length + HAITI_NATIONAL_LENGTH
  ) {
    return `+${digits}`;
  }

  throw new BadRequestException(
    'Phone number must be a Haitian number (8 digits, or 509 followed by 8 digits) ' +
      'or an international number starting with +',
  );
}

/**
 * Ne dit pas si le numéro existe : seulement s'il a une longueur
 * plausible. La plage 8-15 est celle de la recommandation UIT-T E.164.
 */
function assertInternationalLength(digits: string): void {
  if (digits.length < 8 || digits.length > 15) {
    throw new BadRequestException('Phone number must contain between 8 and 15 digits');
  }
}

/**
 * Version affichable d'un numéro déjà canonique : « +509 39 00 00 01 ».
 * Les autres indicatifs sont rendus tels quels, faute de connaître leur
 * découpage national.
 */
export function formatPhone(e164: string): string {
  const match = /^\+509(\d{8})$/.exec(e164);
  if (!match) return e164;
  const n = match[1];
  return `+509 ${n.slice(0, 2)} ${n.slice(2, 4)} ${n.slice(4, 6)} ${n.slice(6, 8)}`;
}
