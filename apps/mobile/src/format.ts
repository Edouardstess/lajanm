// Mise en forme des montants.
//
// Les soldes et montants circulent en unités mineures (centimes) depuis
// le grand livre : les convertir à la volée dans chaque écran, comme le
// faisait le code d'origine, c'est autant d'occasions de se tromper de
// division. La conversion et la mise en forme vivent donc ici.
//
// Le séparateur de milliers est une espace insécable étroite et la
// décimale une virgule : c'est la convention utilisée en Haïti (fr-HT).
// `Intl` n'est pas utilisé : Hermes n'embarque les données de locale que
// si l'app est compilée avec `intl` activé, et le repli silencieux
// donnerait « 12,450.00 » à un utilisateur qui lit « 12 450,00 ».
const THIN_NBSP = ' ';

export function formatMinor(amountMinor: string | number, currency?: string): string {
  const value = Number(amountMinor) / 100;
  return formatAmount(value, currency);
}

export function formatAmount(value: number, currency?: string): string {
  if (!Number.isFinite(value)) return currency ? `— ${currency}` : '—';

  const negative = value < 0;
  const [whole, decimals] = Math.abs(value).toFixed(2).split('.');
  const grouped = whole.replace(/\B(?=(\d{3})+(?!\d))/g, THIN_NBSP);
  const text = `${negative ? '−' : ''}${grouped},${decimals}`;
  return currency ? `${text} ${currency}` : text;
}

/** Sépare la partie entière du reste, pour l'afficher en deux tailles. */
export function splitAmount(amountMinor: string | number): { whole: string; decimals: string } {
  const [whole, decimals] = formatMinor(amountMinor).split(',');
  return { whole, decimals: decimals ?? '00' };
}

/**
 * Le même montant, sans « ,00 » : pour les libellés où la précision au
 * centime n'apporte rien et alourdit la lecture (raccourcis de saisie,
 * jauge de plafond). Un montant qui a des centimes les garde.
 */
export function formatRounded(value: number, currency?: string): string {
  return formatAmount(value, currency).replace(',00', '');
}

/**
 * Date et heure, en jj/mm/aaaa · hh:mm.
 *
 * Les écrans utilisaient `toLocaleString()`, qui suit la langue du
 * téléphone et non celle de l'application : un utilisateur ayant choisi
 * le kreyòl lisait « 9/5/2026, 12:37:00 AM » — un format américain, avec
 * les secondes dont personne n'a besoin, et surtout un jour et un mois
 * qu'on ne peut pas distinguer avant le 13 du mois.
 *
 * Le format est donc fixe et non localisé, dans la convention utilisée en
 * Haïti, et il ne dépend d'aucune donnée de locale (Hermes n'embarque
 * `Intl` que si l'application est compilée avec).
 */
export function formatDateTime(value: string | Date): string {
  const d = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(d.getTime())) return '—';
  return `${formatDate(d)} · ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function formatDate(value: string | Date): string {
  const d = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(d.getTime())) return '—';
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`;
}

function pad(n: number): string {
  return String(n).padStart(2, '0');
}
