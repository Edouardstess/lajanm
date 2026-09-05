import React from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, fonts, radius, spacing, touchTarget, typography } from '../theme';
import { Icon, IconName } from './Icon';

interface Props {
  label: string;
  onPress: () => void;
  loading?: boolean;
  disabled?: boolean;
  variant?: 'primary' | 'secondary' | 'quiet';
  /** Icône devant le libellé (`iconAfter` la place derrière). */
  icon?: IconName;
  iconAfter?: boolean;
}

/**
 * Bouton de l'application.
 *
 * `primary` est doré : c'est la part des 10 % de la règle 60/30/10, et
 * elle est réservée à UNE action par écran. Son libellé est bleu et non
 * blanc — du blanc sur cet or ne donne que 1,99:1, illisible en plein
 * soleil, alors que le bleu monte à 6,48:1.
 *
 * `secondary` (bleu détouré) et `quiet` (surface blanche) portent tout le
 * reste : un écran où plusieurs boutons ont le même poids n'a plus
 * d'action principale, ce qui était le défaut de l'accueil d'origine.
 */
export function PrimaryButton({
  label,
  onPress,
  loading,
  disabled,
  variant = 'primary',
  icon,
  iconAfter,
}: Props) {
  const inactive = disabled || loading;
  // Sur l'or comme sur le blanc, le texte est bleu ; seul le bouton bleu
  // plein prend du blanc — et il n'existe plus comme variante.
  const contentColor = variant === 'primary' ? colors.onAccent : colors.primary;

  const glyph = icon && !loading && <Icon name={icon} size={19} color={contentColor} />;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ disabled: !!inactive, busy: !!loading }}
      onPress={onPress}
      disabled={inactive}
      style={({ pressed }) => [
        styles.button,
        styles[variant],
        pressed && !inactive && (variant === 'primary' ? styles.pressedPrimary : styles.pressed),
        inactive && styles.disabled,
      ]}
    >
      {loading ? (
        <ActivityIndicator color={contentColor} />
      ) : (
        <View style={styles.content}>
          {!iconAfter && glyph}
          <Text style={[styles.label, { color: contentColor }]} numberOfLines={1}>
            {label}
          </Text>
          {iconAfter && glyph}
        </View>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: {
    minHeight: touchTarget.comfortable,
    borderRadius: radius.md,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.lg,
    marginTop: spacing.sm,
  },
  content: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm + 1 },
  primary: { backgroundColor: colors.accent },
  secondary: { backgroundColor: 'transparent', borderWidth: 1.5, borderColor: colors.primary },
  quiet: { backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border },
  // L'état pressé assombrit l'or plutôt que de le rendre transparent :
  // une opacité réduite ferait passer le libellé sous le seuil de
  // contraste au moment précis où l'utilisateur regarde le bouton.
  pressed: { opacity: 0.9 },
  pressedPrimary: { backgroundColor: colors.accentDeep, opacity: 1 },
  disabled: { opacity: 0.45 },
  label: { fontSize: typography.body, fontFamily: fonts.bold, letterSpacing: -0.2 },
});
