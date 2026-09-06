import React from 'react';
import Svg, { Path } from 'react-native-svg';
import { colors } from '../theme';
import { ICON_PATHS, ICON_VIEWBOX, type IconName } from './icons/paths';

export type { IconName };

interface Props {
  name: IconName;
  size?: number;
  color?: string;
  /**
   * Rend l'icône au lecteur d'écran. Par défaut une icône est décorative
   * — elle accompagne un libellé écrit à côté, et l'annoncer reviendrait
   * à faire lire deux fois la même chose.
   */
  label?: string;
}

/**
 * Les icônes de l'application, tirées de Bootstrap Icons.
 *
 * Un seul jeu, une seule facture visuelle (glyphes pleins, grille 16),
 * ce qui règle le défaut de la version précédente : des formes composées
 * à la main avec des `View` et des bordures, dont certaines — le
 * cadenas, la silhouette — restaient illisibles en dessous de 20 px.
 *
 * Les tracés vivent dans `icons/paths.ts`, généré depuis le paquet
 * `bootstrap-icons` par `scripts/generate-icons.js`. Le paquet complet
 * (2 078 icônes) reste une devDependency et n'entre pas dans le binaire.
 */
export function Icon({ name, size = 20, color = colors.text, label }: Props) {
  const paths = ICON_PATHS[name];

  return (
    // Une icône sans libellé ne reçoit aucune propriété
    // d'accessibilité : elle ne contient pas de texte, donc rien à
    // annoncer. `importantForAccessibility` et
    // `accessibilityElementsHidden` ont été retirés — react-native-svg
    // les transmet tels quels au <svg> du DOM sur le web, où React
    // avertit qu'il ne les reconnaît pas.
    <Svg
      width={size}
      height={size}
      viewBox={`0 0 ${ICON_VIEWBOX} ${ICON_VIEWBOX}`}
      accessible={label ? true : undefined}
      accessibilityRole={label ? 'image' : undefined}
      accessibilityLabel={label}
    >
      {paths.map((p, i) => (
        <Path key={i} d={p.d} fill={color} fillRule={'fillRule' in p ? p.fillRule : undefined} />
      ))}
    </Svg>
  );
}
