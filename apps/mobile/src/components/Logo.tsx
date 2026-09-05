import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, fonts, spacing, typography } from '../theme';

interface Props {
  /** `full` ajoute la signature sous le logotype. */
  variant?: 'full' | 'wordmark';
  size?: 'md' | 'lg';
}

/**
 * Le logotype Lajan'm.
 *
 * C'est la moitié typographique du logo de la charte, reproduite à
 * l'identique : Montserrat, « LAJAN' » en bleu, le « M » final en or, et
 * la signature entre deux filets dorés. Police et couleurs sont celles de
 * la charte, donc ce n'est pas une approximation.
 *
 * Le monogramme graphique (le LM avec la flèche et les pièces) n'est pas
 * redessiné ici : le redessiner de mémoire produirait une contrefaçon.
 * Pour l'ajouter, déposer le fichier dans `assets/logo-mark.png` et
 * l'insérer au-dessus du logotype — voir docs/brand.md.
 */
export function Logo({ variant = 'full', size = 'md' }: Props) {
  const large = size === 'lg';
  return (
    <View style={styles.root}>
      <Text
        style={[styles.wordmark, large && styles.wordmarkLarge]}
        accessibilityRole="header"
        // Le logotype est lu comme un mot, pas épelé lettre par lettre.
        accessibilityLabel="Lajan'm"
      >
        LAJAN<Text style={styles.apostrophe}>’</Text>
        <Text style={styles.accentLetter}>M</Text>
      </Text>

      {variant === 'full' && (
        <View style={styles.taglineRow}>
          <View style={styles.rule} />
          <Text style={styles.tagline}>VOTRE ARGENT, VOS OPPORTUNITÉS</Text>
          <View style={styles.rule} />
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  root: { alignItems: 'center' },
  wordmark: {
    fontFamily: fonts.black,
    fontSize: 34,
    color: colors.primary,
    letterSpacing: -0.5,
    lineHeight: 42,
  },
  // Modificateur : il hérite de la graisse de `wordmark`, ne pas la redéfinir.
  wordmarkLarge: { fontSize: 44, lineHeight: 54 },
  apostrophe: { color: colors.accent },
  accentLetter: { color: colors.accent },
  taglineRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    marginTop: spacing.xs + 2,
  },
  // Les filets dorés : l'accent employé comme trait, jamais comme texte
  // — l'or sur blanc plafonne à 1,99:1 et serait illisible.
  //
  // Largeur fixe et non `flex: 1` : la signature est plus large que le
  // logotype, donc des filets élastiques se retrouvaient à zéro pixel et
  // disparaissaient purement et simplement.
  rule: { width: 26, height: 1.5, backgroundColor: colors.accent },
  tagline: {
    fontFamily: fonts.semibold,
    fontSize: typography.overline - 3,
    color: colors.muted,
    letterSpacing: 1.4,
  },
});
