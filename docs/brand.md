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

**Ce qui manque encore**, et qui demande les fichiers sources :

1. **Le monogramme graphique** (le LM avec la flèche et les pièces). Il
   n'est pas redessiné : le reproduire de mémoire donnerait une
   contrefaçon. Pour l'ajouter, déposer `apps/mobile/assets/logo-mark.png`
   (ou `.svg`) et l'insérer au-dessus du logotype dans `Logo.tsx`.

2. **Les icônes d'application.** Les fichiers de `apps/mobile/assets/`
   sont encore ceux du gabarit Expo :

   | Fichier | Usage | Format attendu |
   |---|---|---|
   | `icon.png` | icône iOS | 1024×1024, sans transparence |
   | `android-icon-foreground.png` | icône adaptative Android | 1024×1024, sujet dans les 66 % centraux |
   | `android-icon-background.png` | fond de l'icône adaptative | 1024×1024, aplat `#0B2D5B` |
   | `android-icon-monochrome.png` | thème Material You | 1024×1024, silhouette unie |
   | `splash-icon.png` | écran de démarrage | 1024×1024 |
   | `favicon.png` | web | 48×48 |

   Les couleurs de fond sont déjà réglées sur la charte dans
   `app.config.ts` ; seuls les visuels restent à remplacer. **À faire avant
   toute publication sur les stores.**

3. **L'écran de démarrage** relève depuis le SDK 54 du plugin
   `expo-splash-screen`, qui n'est pas installé. À ajouter en même temps
   que les vraies icônes.
