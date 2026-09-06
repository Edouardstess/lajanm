#!/usr/bin/env node
/**
 * Fabrique les visuels de marque (écran de lancement et icônes) à partir
 * du logotype, rendus avec la vraie Montserrat.
 *
 * Pourquoi un script plutôt que des fichiers déposés à la main : les
 * six formats attendus par Expo se déduisent tous du même dessin. Les
 * régénérer d'une commande évite qu'ils divergent, et documente
 * exactement ce qui a produit chaque pixel.
 *
 *   node scripts/generate-brand-assets.mjs
 *
 * Nécessite Playwright et un Chromium (utilisés uniquement au moment de
 * la génération, jamais à l'exécution de l'application).
 */
import { chromium } from 'playwright';
import { readFileSync, writeFileSync } from 'fs';
import { dirname, join, resolve } from 'path';
import { fileURLToPath } from 'url';

const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(here, '../../..');
const assets = resolve(here, '../assets');

// Charte de marque — les mêmes valeurs que src/theme.ts.
const NAVY = '#0B2D5B';
const GOLD = '#D4AF37';
const WHITE = '#FFFFFF';

function fontFace(weight, file) {
  const ttf = readFileSync(join(repoRoot, 'node_modules/@expo-google-fonts/montserrat', file));
  return `@font-face{font-family:Montserrat;font-weight:${weight};src:url(data:font/ttf;base64,${ttf.toString('base64')}) format('truetype');}`;
}

const FONTS =
  fontFace(800, '800ExtraBold/Montserrat_800ExtraBold.ttf') +
  fontFace(600, '600SemiBold/Montserrat_600SemiBold.ttf');

/**
 * Le logotype : « LAJAN' » puis un « M » doré, la signature entre deux
 * filets. Identique au composant Logo.tsx, au support près.
 */
function wordmark({ size, color, accent, tagline, scale = 1 }) {
  return `
    <div class="mark" style="transform:scale(${scale})">
      <div class="word" style="color:${color};font-size:${size}px">LAJAN<span style="color:${accent}">’M</span></div>
      ${
        tagline
          ? `<div class="tag">
               <i style="background:${accent}"></i>
               <span style="color:${color};opacity:.72;font-size:${size * 0.115}px">VOTRE ARGENT, VOS OPPORTUNITÉS</span>
               <i style="background:${accent}"></i>
             </div>`
          : ''
      }
    </div>`;
}

/**
 * Le monogramme, pour les formats carrés.
 *
 * « LAJAN'M » en entier occupe une bande très large : réduit au carré
 * d'une icône d'application, il tombe à quelques pixels de haut et
 * devient illisible sur un écran d'accueil. Les initiales, dans la
 * police de la charte et avec le M doré, restent lisibles à 48 px.
 *
 * Ce n'est PAS le monogramme illustré de la charte (le LM avec la flèche
 * et les pièces) : celui-là demande le fichier source. C'est une marque
 * de remplacement dérivée de la charte, à remplacer dès que le vrai
 * dessin est disponible — voir docs/brand.md.
 */
function monogram({ size, color, accent }) {
  return `
    <div class="mark">
      <div class="word" style="color:${color};font-size:${size}px;letter-spacing:-.045em">L<span style="color:${accent}">M</span></div>
    </div>`;
}

function page({ width, height, background, body }) {
  return `<!doctype html><meta charset="utf-8"><style>
    ${FONTS}
    *{margin:0;padding:0;box-sizing:border-box}
    html,body{width:${width}px;height:${height}px}
    body{background:${background};display:flex;align-items:center;justify-content:center;
         font-family:Montserrat,sans-serif;-webkit-font-smoothing:antialiased}
    .mark{display:flex;flex-direction:column;align-items:center}
    .word{font-weight:800;letter-spacing:-.02em;line-height:1}
    .tag{display:flex;align-items:center;gap:.7em;margin-top:.5em}
    .tag i{display:block;width:2.2em;height:2px;border-radius:2px;opacity:.75}
    .tag span{font-weight:600;letter-spacing:.16em;white-space:nowrap}
  </style>${body}`;
}

const TARGETS = [
  {
    // Image de l'écran de lancement.
    //
    // Le MONOGRAMME et non le logotype : depuis Android 12, le système
    // affiche cette image dans un masque CIRCULAIRE. Un logotype large y
    // serait rogné aux deux bouts, et « LAJAN'M » deviendrait « AJAN' ».
    // Le logotype complet, lui, s'affiche à l'intérieur de l'application
    // (composant Logo) une fois le premier écran dessiné.
    //
    // Fond transparent : le plugin pose le bleu de la charte derrière.
    file: 'splash-icon.png',
    width: 1024,
    height: 1024,
    background: 'transparent',
    body: monogram({ size: 400, color: WHITE, accent: GOLD }),
  },
  {
    // iOS refuse une icône avec transparence : fond plein obligatoire.
    file: 'icon.png',
    width: 1024,
    height: 1024,
    background: NAVY,
    // 420 et non 560 : iOS arrondit les angles et Android applique un
    // masque, donc une marque qui touche les bords se fait rogner.
    body: monogram({ size: 420, color: WHITE, accent: GOLD }),
  },
  {
    // Android, plan avant : le sujet doit tenir dans les 66 % centraux,
    // le système rogne le reste selon la forme choisie par le téléphone.
    file: 'android-icon-foreground.png',
    width: 1024,
    height: 1024,
    background: 'transparent',
    body: monogram({ size: 400, color: WHITE, accent: GOLD }),
  },
  {
    file: 'android-icon-background.png',
    width: 1024,
    height: 1024,
    background: NAVY,
    body: '',
  },
  {
    // Material You : une silhouette d'une seule couleur, que le système
    // recolore. Pas d'or ici, il serait ignoré.
    file: 'android-icon-monochrome.png',
    width: 1024,
    height: 1024,
    background: 'transparent',
    body: monogram({ size: 400, color: WHITE, accent: WHITE }),
  },
  {
    file: 'favicon.png',
    width: 96,
    height: 96,
    background: NAVY,
    body: monogram({ size: 52, color: WHITE, accent: GOLD }),
  },
];

const browser = await chromium.launch({
  executablePath: process.env.CHROMIUM_PATH || undefined,
  args: ['--no-sandbox'],
});

for (const target of TARGETS) {
  const context = await browser.newContext({
    viewport: { width: target.width, height: target.height },
    deviceScaleFactor: 1,
  });
  const p = await context.newPage();
  await p.setContent(page(target), { waitUntil: 'load' });
  await p.evaluate(() => document.fonts.ready);
  const buffer = await p.screenshot({
    omitBackground: target.background === 'transparent',
  });
  writeFileSync(join(assets, target.file), buffer);
  console.log(`${target.file.padEnd(32)} ${target.width}x${target.height}  ${(buffer.length / 1024).toFixed(0)} Ko`);
  await context.close();
}

await browser.close();
