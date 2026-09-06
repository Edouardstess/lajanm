#!/usr/bin/env node
/**
 * Extrait les tracés des icônes Bootstrap réellement utilisées, vers
 * src/components/icons/paths.ts.
 *
 * Pourquoi générer plutôt qu'importer : le paquet `bootstrap-icons`
 * contient 2 078 fichiers SVG. En embarquer la totalité dans un binaire
 * mobile pour en afficher quinze serait absurde ; il reste donc une
 * devDependency, et seuls les tracés listés ci-dessous entrent dans le
 * dépôt.
 *
 * Régénérer après avoir ajouté une entrée à ICONS :
 *   node scripts/generate-icons.js
 */
const fs = require('fs');
const path = require('path');

// Nom fonctionnel dans l'app  ->  nom du fichier Bootstrap.
// Le nom de gauche décrit ce que l'icône VEUT DIRE ici, celui de droite
// le glyphe : changer de glyphe ne doit pas obliger à toucher aux écrans.
const ICONS = {
  'arrow-up': 'arrow-up',
  'arrow-down': 'arrow-down',
  'arrow-right': 'arrow-right',
  send: 'send',
  check: 'check-lg',
  clock: 'clock',
  lock: 'lock',
  shield: 'shield-lock',
  person: 'person-fill',
  home: 'house',
  help: 'question-circle',
  card: 'credit-card-2-front',
  alert: 'exclamation-circle',
  plus: 'plus-lg',
};

const iconsDir = path.resolve(__dirname, '../../../node_modules/bootstrap-icons/icons');
if (!fs.existsSync(iconsDir)) {
  console.error(
    'bootstrap-icons introuvable. Installer la devDependency :\n' +
      '  npm install --save-dev --workspace @lajanm/mobile bootstrap-icons',
  );
  process.exit(1);
}

/** Chaque <path> devient { d, fillRule? } ; le reste du SVG est ignoré. */
function extract(file) {
  const svg = fs.readFileSync(file, 'utf8');
  const paths = [];
  for (const tag of svg.matchAll(/<path\b[^>]*>/g)) {
    const d = /\bd="([^"]+)"/.exec(tag[0]);
    if (!d) continue;
    const rule = /\bfill-rule="([^"]+)"/.exec(tag[0]);
    paths.push(rule ? { d: d[1], fillRule: rule[1] } : { d: d[1] });
  }
  if (paths.length === 0) throw new Error(`aucun tracé dans ${file}`);
  return paths;
}

const entries = Object.entries(ICONS).map(([name, file]) => {
  const full = path.join(iconsDir, `${file}.svg`);
  if (!fs.existsSync(full)) throw new Error(`icône Bootstrap inconnue : ${file}`);
  return [name, file, extract(full)];
});

const body = entries
  .map(([name, file, paths]) => {
    const key = /^[a-z][a-zA-Z0-9]*$/.test(name) ? name : `'${name}'`;
    const rendered = paths
      .map((p) => `    { d: '${p.d.replace(/'/g, "\\'")}'${p.fillRule ? `, fillRule: '${p.fillRule}' as const` : ''} },`)
      .join('\n');
    return `  // bootstrap-icons/${file}.svg\n  ${key}: [\n${rendered}\n  ],`;
  })
  .join('\n');

const out = `// FICHIER GÉNÉRÉ — ne pas modifier à la main.
// Source : bootstrap-icons ${require('bootstrap-icons/package.json').version}
// Régénérer : node scripts/generate-icons.js
//
// Tous les tracés sont dessinés dans une zone de 16 x 16 et remplis avec
// la couleur passée au composant Icon.

export const ICON_VIEWBOX = 16;

export const ICON_PATHS = {
${body}
} as const;

export type IconName = keyof typeof ICON_PATHS;
`;

const target = path.resolve(__dirname, '../src/components/icons/paths.ts');
fs.mkdirSync(path.dirname(target), { recursive: true });
fs.writeFileSync(target, out);
console.log(`${entries.length} icônes écrites dans ${path.relative(process.cwd(), target)}`);
