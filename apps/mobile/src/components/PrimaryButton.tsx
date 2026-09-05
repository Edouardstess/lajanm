import React from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, radius, spacing, touchTarget, typography } from '../theme';
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
 * `quiet` a été ajouté pour les actions de navigation secondaires : un
 * écran où six boutons ont tous une bordure verte n'a plus d'action
 * principale, ce qui était précisément le défaut de l'accueil d'origine.
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
  const contentColor =
    variant === 'primary' ? colors.primaryText : variant === 'quiet' ? colors.text : colors.primary;

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
        pressed && !inactive && styles.pressed,
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
  primary: { backgroundColor: colors.primary },
  secondary: { backgroundColor: 'transparent', borderWidth: 1.5, borderColor: colors.primary },
  quiet: { backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border },
  pressed: { opacity: 0.85 },
  disabled: { opacity: 0.45 },
  label: { fontSize: typography.body, fontWeight: '700', letterSpacing: -0.2 },
});
