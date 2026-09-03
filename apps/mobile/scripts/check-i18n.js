#!/usr/bin/env node
/**
 * Fails the build when a translation key is missing.
 *
 * Two distinct failure modes, both invisible at runtime (the `t()` helper
 * falls back to the default locale and then to echoing the raw key, so a
 * typo ships as a user seeing "support.faq_title" on screen):
 *
 *   1. a key referenced in code that no locale defines;
 *   2. locale drift — a key present in some locales but not others, which
 *      silently serves French/English users the Kreyòl string.
 *
 * Deliberately dependency-free and run from the mobile `lint` script: the
 * app has no test runner, and pulling one in for this single check would
 * cost more than it's worth.
 */
const fs = require('fs');
const path = require('path');

const appRoot = path.resolve(__dirname, '..');
const srcDir = path.join(appRoot, 'src');
const localesDir = path.join(srcDir, 'i18n', 'locales');

/**
 * Keys built at runtime from a template literal — t(`support.status_${s}`)
 * — can't be found by scanning for string literals, so the prefixes and
 * their possible suffixes are listed here. Keep in sync when adding a new
 * dynamic key family.
 */
const DYNAMIC_KEYS = {
  'support.status_': ['open', 'in_progress', 'resolved', 'closed'],
  'support.category_': ['general', 'transaction', 'kyc', 'technical', 'other'],
  'wallet.type_': ['topup', 'payout', 'transfer', 'adjustment'],
};

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) return walk(full);
    return /\.tsx?$/.test(entry.name) ? [full] : [];
  });
}

/** Locale files are pure object literals, so they can be evaluated directly. */
function loadCatalog(file) {
  const source = fs
    .readFileSync(file, 'utf8')
    .replace(/^\s*\/\/.*$/gm, '')
    .replace('export default', 'return');
  return new Function(source)();
}

function flatten(catalog, prefix = '') {
  return Object.entries(catalog).flatMap(([key, value]) =>
    value && typeof value === 'object'
      ? flatten(value, `${prefix}${key}.`)
      : [`${prefix}${key}`],
  );
}

const locales = fs
  .readdirSync(localesDir)
  .filter((f) => f.endsWith('.ts'))
  .map((f) => path.basename(f, '.ts'));

const catalogs = {};
const keySets = {};
for (const locale of locales) {
  catalogs[locale] = loadCatalog(path.join(localesDir, `${locale}.ts`));
  keySets[locale] = new Set(flatten(catalogs[locale]));
}

// Keys actually referenced by the app.
const used = new Set();
for (const file of walk(srcDir)) {
  const contents = fs.readFileSync(file, 'utf8');
  for (const match of contents.matchAll(/\bt\(\s*'([a-zA-Z0-9_]+\.[a-zA-Z0-9_]+)'\s*\)/g)) {
    used.add(match[1]);
  }
}
for (const [prefix, suffixes] of Object.entries(DYNAMIC_KEYS)) {
  for (const suffix of suffixes) used.add(prefix + suffix);
}

const problems = [];

for (const key of [...used].sort()) {
  for (const locale of locales) {
    if (!keySets[locale].has(key)) {
      problems.push(`missing key used by the app: [${locale}] ${key}`);
    }
  }
}

// Locale drift, in both directions, against the union of all keys.
const allKeys = new Set(locales.flatMap((l) => [...keySets[l]]));
for (const key of [...allKeys].sort()) {
  const absent = locales.filter((l) => !keySets[l].has(key));
  if (absent.length > 0 && absent.length < locales.length) {
    problems.push(`locale drift: "${key}" is missing from ${absent.join(', ')}`);
  }
}

if (problems.length > 0) {
  console.error('i18n check failed:\n' + problems.map((p) => `  - ${p}`).join('\n'));
  process.exit(1);
}

console.log(
  `i18n check passed: ${used.size} keys referenced by the app, ` +
    `${allKeys.size} keys defined, ${locales.length} locales (${locales.join(', ')}), no drift.`,
);
