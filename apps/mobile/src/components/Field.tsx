import React, { forwardRef, useState } from 'react';
import { StyleSheet, Text, TextInput, TextInputProps, View } from 'react-native';
import { colors, fonts, radius, spacing, touchTarget, typography } from '../theme';

interface Props extends TextInputProps {
  label: string;
  /** Précision courte sous le libellé (format attendu, limite...). */
  hint?: string;
  /** Message d'erreur propre au champ, annoncé avec lui. */
  error?: string;
}

/**
 * Champ de saisie avec son libellé. Trois choses que le code d'origine
 * répétait dans chaque écran, à chaque fois un peu différemment :
 * l'`accessibilityLabel` reprend le libellé visible, l'état de focus est
 * visible (bordure verte + halo, pas seulement une nuance de gris), et
 * l'erreur est rattachée au champ plutôt qu'au bas du formulaire.
 */
export const Field = forwardRef<TextInput, Props>(function Field(
  { label, hint, error, style, onFocus, onBlur, ...rest },
  ref,
) {
  const [focused, setFocused] = useState(false);

  return (
    <View style={styles.wrapper}>
      <Text style={styles.label}>{label}</Text>
      {hint && <Text style={styles.hint}>{hint}</Text>}
      <TextInput
        ref={ref}
        accessibilityLabel={label}
        placeholderTextColor={colors.placeholder}
        {...rest}
        onFocus={(e) => {
          setFocused(true);
          onFocus?.(e);
        }}
        onBlur={(e) => {
          setFocused(false);
          onBlur?.(e);
        }}
        style={[
          styles.input,
          focused && styles.inputFocused,
          error != null && styles.inputError,
          rest.editable === false && styles.inputDisabled,
          style,
        ]}
      />
      {error != null && <Text style={styles.error}>{error}</Text>}
    </View>
  );
});

const styles = StyleSheet.create({
  wrapper: { marginBottom: spacing.md },
  label: { fontSize: typography.label, fontFamily: fonts.semibold, color: colors.text, marginBottom: spacing.sm },
  hint: { fontSize: typography.overline, fontFamily: fonts.regular, color: colors.muted, marginTop: -spacing.xs, marginBottom: spacing.sm },
  input: {
    minHeight: touchTarget.comfortable,
    backgroundColor: colors.surface,
    borderWidth: 1.5,
    borderColor: colors.border,
    borderRadius: radius.md,
    paddingHorizontal: spacing.md,
    fontSize: typography.body, fontFamily: fonts.regular,
    color: colors.text,
  },
  inputFocused: { borderColor: colors.primary },
  inputError: { borderColor: colors.danger },
  inputDisabled: { backgroundColor: colors.surfaceAlt, color: colors.muted },
  error: { fontSize: typography.overline, fontFamily: fonts.regular, color: colors.danger, marginTop: spacing.xs },
});
