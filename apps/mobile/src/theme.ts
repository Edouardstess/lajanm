// Jetons de design partagés.
//
// Les contraintes viennent du brief produit, pas d'un goût graphique :
// public à faible littératie numérique, réseaux 2G/EDGE, écrans souvent
// lus en plein soleil. D'où des cibles tactiles larges, des contrastes
// élevés et peu de texte par écran.
//
// La palette est chaude — crème, vert forêt, or — plutôt que le blanc
// clinique d'origine : sur un écran bon marché en extérieur, un fond
// crème fatigue moins qu'un blanc pur, et l'or réserve un signal
// distinct du vert « tout va bien » et du rouge « échec ».
export const colors = {
  // Surfaces
  ground: '#FBF8F3', // fond d'écran
  surface: '#FFFFFF', // cartes, champs
  surfaceAlt: '#F4F1EA', // encarts d'information
  background: '#FBF8F3', // conservé : nom utilisé par le code existant

  // Marque
  primary: '#0E5C42',
  primaryDeep: '#0B4D37',
  primaryText: '#FFFFFF',
  primarySoft: '#E4F0EA',

  // Accent — jamais un état, seulement une mise en valeur ou une attente
  accent: '#C98A2B',
  accentSoft: '#F5E7CD',
  accentText: '#8A5D14',

  // Texte
  text: '#1A1714',
  muted: '#6E655C',
  placeholder: '#A79D91',

  // Bordures
  border: '#E8E0D5',
  borderStrong: '#D6CCBD',

  // États. `success` reste le vert de marque : dans un portefeuille,
  // « ça a marché » et « c'est la marque » sont volontairement le même
  // vert, alors que l'attente (or) et l'échec (rouge) s'en détachent.
  success: '#0E5C42',
  successSoft: '#E4F0EA',
  danger: '#B3261E',
  dangerSoft: '#FBEAE8',
  warning: '#C98A2B',
  warningSoft: '#F5E7CD',
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

// 52 px reste le minimum absolu (WCAG 2.5.5 en demande 44 ; on va
// au-delà parce que l'app se manipule souvent d'une main, debout).
// `comfortable` est la hauteur des champs et boutons principaux.
export const touchTarget = { minHeight: 52, comfortable: 56 } as const;

// Ombre discrète et unique : sur Android une élévation forte fait
// baver les bords sur les dalles bon marché.
export const elevation = {
  card: {
    shadowColor: '#1A1714',
    shadowOpacity: 0.05,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 2 },
    elevation: 1,
  },
} as const;
