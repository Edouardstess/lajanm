# Charte de marque appliquée à l'application mobile

Ce document dit ce qui est implémenté, ce qui ne l'est pas, et pourquoi
l'application s'écarte de la planche de marque sur un point précis.

## Les trois couleurs

| Couleur | Hex | Rôle dans la charte | Rôle dans l'application |
|---|---|---|---|
| Bleu Premium | `#0B2D5B` | Confiance, sécurité | Structure : carte de solde, texte, en-têtes, icônes |
| Or | `#D4AF37` | Prospérité, croissance | Accent : l'action principale, et elle seule |
| Blanc | `#F8F9FA` | Simplicité, modernité | Fond de tous les écrans et surfaces des cartes |

Elles vivent dans `apps/mobile/src/theme.ts`. Aucun écran ne contient de
valeur hexadécimale en dur.

## Règle 60 / 30 / 10

- **60 % blanc** — fond d'écran et surfaces. C'est aussi le bon choix
  technique : sur une dalle bon marché en plein soleil, un fond clair
  reste lisible là où un fond sombre devient un miroir.
- **30 % bleu** — tout ce qui porte du sens : le solde, les libellés, les
  icônes, les boutons secondaires.
- **10 % or** — **une** action principale par écran. Les états
  (sélection, attente) utilisent des teintes d'or très claires
  (`accentSoft`), pas l'or plein. Dès que deux éléments portent l'or plein
  au même niveau sur un écran, la règle est rompue.

Contrôle rapide, écran par écran : compter les aplats `colors.accent`. S'il
y en a plus d'un, c'est un défaut.

## Contraste : ce que l'or ne peut pas faire

Mesures WCAG 2.1 sur les couleurs de la charte :

| Paire | Ratio | Verdict |
|---|---|---|
| bleu sur blanc | 12,93:1 | AAA |
| blanc sur bleu | 12,93:1 | AAA |
| bleu sur or | 6,48:1 | AA |
| **or sur blanc** | **1,99:1** | **échec** |
| **blanc sur or** | **1,99:1** | **échec** |

Deux conséquences non négociables :

1. **L'or ne porte jamais de texte sur fond blanc.** Il ne sert que de
   remplissage ou de filet. Pour un texte doré, utiliser `colors.accentInk`
   (`#8A6D1F`, 4,65:1).
2. **Un bouton doré porte un libellé bleu, jamais blanc.**

Le point 2 est **le seul écart assumé avec la planche de marque**, où la
maquette du téléphone montre un bouton doré à texte clair. Ce contraste-là
(1,99:1) est illisible au soleil pour le public visé. Le bleu sur or monte
à 6,48:1 et reste dans les couleurs de la marque.

Deux teintes ont également dû être assombries pour passer AA : le vert de
succès (`#1B7F4C` → `#146638`) et le texte d'aide des champs
(`#8E9BB0` → `#68768F`).

## Typographie

Montserrat, embarquée dans le binaire via `@expo-google-fonts/montserrat`
— pas téléchargée : sur un réseau EDGE, une police distante arrive après le
premier écran, ou jamais.

Cinq graisses sont chargées dans `App.tsx` : 400, 500, 600, 700 et 800
(cette dernière réservée au logotype). En React Native, `fontWeight` est
ignoré pour une police personnalisée ; chaque graisse est une famille
distincte, listée dans `fonts` (`theme.ts`).

Le séparateur de milliers est U+00A0 et non U+202F : Montserrat ne contient
pas l'espace fine insécable, et le séparateur disparaissait dans les petits
corps.

## Logo

`apps/mobile/src/components/Logo.tsx` rend le **logotype** : « LAJAN' » en
bleu, « M » en or, la signature entre deux filets dorés — police et
couleurs de la charte, donc pas une approximation.

### Les visuels générés

`apps/mobile/scripts/generate-brand-assets.mjs` produit les six fichiers
d'`assets/` à partir du même dessin, avec la vraie Montserrat :

```bash
node apps/mobile/scripts/generate-brand-assets.mjs
```

| Fichier | Contenu | Pourquoi |
|---|---|---|
| `splash-icon.png` | monogramme, transparent | Android 12+ masque cette image dans un **cercle** ; un logotype large y serait rogné |
| `icon.png` | monogramme sur bleu plein | iOS refuse la transparence |
| `android-icon-foreground.png` | monogramme, transparent | plan avant de l'icône adaptative |
| `android-icon-background.png` | aplat `#0B2D5B` | plan arrière |
| `android-icon-monochrome.png` | monogramme blanc uni | Material You recolore la silhouette |
| `favicon.png` | monogramme, 96×96 | web |

### Le monogramme est provisoire

Les icônes utilisent **« LM » composé en Montserrat**, pas le monogramme
illustré de la charte (le LM avec la flèche et les pièces). Ce dernier
demande son fichier source : le redessiner de mémoire donnerait une
contrefaçon, pas votre logo.

Pour le remplacer : déposer `apps/mobile/assets/logo-mark.png` (ou `.svg`,
1024×1024, fond transparent) et remplacer les appels à `monogram()` par
cette image dans le script, puis régénérer.

### L'écran de lancement

Configuré par le plugin `expo-splash-screen` dans `app.config.ts` : fond
`#0B2D5B` sur les deux thèmes, monogramme centré à 180 px.

`App.tsx` retient l'écran de lancement (`preventAutoHideAsync`) jusqu'à ce
que Montserrat soit chargée, et ne le retire qu'à la **pose** de la vue
racine — pas dans un effet, qui s'exécute avant que la première image ne
soit dessinée et laisserait apparaître une frame vide. C'est cette
continuité, plus que le dessin, qui fait la différence entre un lancement
soigné et un lancement bricolé.
