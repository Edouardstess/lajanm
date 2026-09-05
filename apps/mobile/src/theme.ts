// Jetons de design de Lajan'm.
//
// La palette n'est pas un choix esthétique : ce sont les trois couleurs de
// la charte de marque, avec leur rôle d'origine.
//
//   #0B2D5B  Bleu Premium   Confiance | Sécurité
//   #D4AF37  Or             Prospérité | Croissance
//   #F8F9FA  Blanc          Simplicité | Modernité
//
// ─── Règle 60 / 30 / 10 ────────────────────────────────────────────────
//
//   60 %  BLANC     fond de tous les écrans et surface des cartes.
//                   C'est aussi le bon choix technique : sur une dalle
//                   bon marché en plein soleil, un fond clair reste
//                   lisible là où un fond sombre devient un miroir.
//
//   30 %  BLEU      la structure — carte de solde, texte, en-têtes,
//                   icônes, bouton secondaire. Le bleu porte le sens,
//                   jamais la décoration.
//
//   10 %  OR        l'accent, et rien d'autre : UNE action principale par
//                   écran, l'état sélectionné, le filet du logo, le badge
//                   de compte vérifié. Dès que l'or apparaît deux fois
//                   avec le même poids sur un écran, la règle est rompue
//                   et il faut en retirer un.
//
// ─── Contraste : ce que l'or ne peut pas faire ─────────────────────────
//
// Mesuré (WCAG 2.1) :
//   bleu sur blanc  12,93:1  AAA
//   blanc sur bleu  12,93:1  AAA
//   bleu sur or      6,48:1  AA
//   or sur blanc     1,99:1  ÉCHEC
//   blanc sur or     1,99:1  ÉCHEC
//
// Deux conséquences non négociables :
//   1. l'or ne porte JAMAIS de texte sur fond blanc — il ne sert que de
//      remplissage ou de filet ;
//   2. un bouton or porte un libellé BLEU, jamais blanc. C'est le seul
//      écart assumé avec la planche de marque, où le bouton doré porte un
//      texte clair : ce contraste-là est illisible au soleil.
export const colors = {
  // ---- 60 % ----
  ground: '#F8F9FA', // fond d'écran
  surface: '#FFFFFF', // cartes et champs, posés sur le fond
  surfaceAlt: '#EEF1F5', // encarts d'information
  background: '#F8F9FA', // alias conservé pour le code existant

  // ---- 30 % ----
  primary: '#0B2D5B',
  primaryDeep: '#071E3E', // dégradé bas de la carte de solde
  primaryText: '#FFFFFF',
  primarySoft: '#E7ECF3', // fond d'icône, teinte du bleu

  // ---- 10 % ----
  accent: '#D4AF37',
  accentDeep: '#B8942C', // bordure et état pressé de l'or
  // Or assombri, réservé aux ICÔNES posées sur accentSoft : l'or de la
  // charte n'y atteint que 2,62:1, sous le seuil de 3:1 des éléments non
  // textuels. 4,46:1 ici, donc lisible aussi si le texte l'emprunte.
  accentInk: '#8A6D1F',
  accentSoft: '#FBF4E2', // fond très clair, jamais du texte dessus
  onAccent: '#0B2D5B', // le seul texte admis sur de l'or

  // ---- Texte ----
  text: '#0B2D5B',
  muted: '#5A6B85', // 5,14:1 sur le fond — au-dessus du seuil AA
  // 4,59:1 sur blanc. La nuance d'origine (#8E9BB0) tombait à 2,81:1 :
  // un texte d'aide illisible au soleil n'aide personne.
  placeholder: '#68768F',

  // ---- Bordures ----
  border: '#E2E6EC',
  borderStrong: '#C9D2DE',

  // ---- États ----
  // Le succès est vert et non bleu : « c'est passé » doit se distinguer
  // de la couleur de marque, sinon tout l'écran a l'air d'un succès.
  // #1B7F4C plafonnait à 4,39:1 sur son propre fond clair — sous le seuil
  // AA. Assombri jusqu'à 6,16:1.
  success: '#146638',
  successSoft: '#E6F3EC',
  // L'attente emprunte l'or : c'est le seul cas où l'accent porte un état,
  // et il reste cohérent avec « croissance en cours ».
  warning: '#B8942C',
  warningSoft: '#FBF4E2',
  danger: '#C62828',
  dangerSoft: '#FBEAEA',
} as const;

// Montserrat, la police de la charte. En React Native, `fontWeight` est
// ignoré pour une police personnalisée : chaque graisse est une famille
// distincte, d'où cette table plutôt que des poids numériques.
export const fonts = {
  regular: 'Montserrat_400Regular',
  medium: 'Montserrat_500Medium',
  semibold: 'Montserrat_600SemiBold',
  bold: 'Montserrat_700Bold',
  black: 'Montserrat_800ExtraBold', // réservé au logotype
} as const;

export const spacing = { xs: 4, sm: 8, md: 16, lg: 24, xl: 32 } as const;

export const radius = { sm: 12, md: 16, lg: 20, xl: 24, pill: 999 } as const;

export const typography = {
  display: 38, // le solde, et lui seul
  amount: 34, // la saisie d'un montant
  title: 24,
  heading: 20,
  body: 17,
  label: 15,
  caption: 13,
  overline: 12,
} as const;

// 52 px reste le minimum absolu (WCAG 2.5.5 en demande 44 ; Material 48) ;
// on va au-delà parce que l'app se manipule souvent d'une main, debout.
export const touchTarget = { minHeight: 52, comfortable: 56 } as const;

// Ombre discrète et unique : sur Android une élévation forte fait baver
// les bords sur les dalles bon marché.
export const elevation = {
  card: {
    shadowColor: '#0B2D5B',
    shadowOpacity: 0.06,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 2 },
    elevation: 1,
  },
} as const;
