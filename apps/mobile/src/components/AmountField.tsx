import React, { useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { formatAmount, formatRounded } from '../format';
import { colors, radius, spacing, touchTarget, typography } from '../theme';

interface Props {
  label: string;
  value: string;
  onChangeText: (value: string) => void;
  currency?: string;
  /** Montants proposés en un geste, dans l'unité principale (HTG). */
  presets?: number[];
  editable?: boolean;
  error?: string;
}

/**
 * Saisie d'un montant. Le champ est la pièce centrale de trois écrans
 * (envoi, dépôt, retrait) : il est donc traité comme tel — chiffre en
 * grand, et des montants fréquents accessibles d'un seul appui.
 *
 * Les raccourcis ne sont pas un confort : taper « 2000 » sur un clavier
 * numérique, debout, avec une main, est la première source de montant
 * erroné. Ils remplissent le champ, qui reste modifiable.
 */
export function AmountField({
  label,
  value,
  onChangeText,
  currency = 'HTG',
  presets,
  editable = true,
  error,
}: Props) {
  const [focused, setFocused] = useState(false);

  // On ne garde que les chiffres : un montant en unité principale ne se
  // saisit ni avec un signe, ni avec un séparateur qui varierait selon
  // le clavier du téléphone.
  const onChange = (next: string) => onChangeText(next.replace(/[^0-9]/g, ''));

  return (
    <View>
      <View
        style={[
          styles.box,
          (focused || value.length > 0) && styles.boxActive,
          error != null && styles.boxError,
          !editable && styles.boxDisabled,
        ]}
      >
        <Text style={styles.label}>{label}</Text>
        <View style={styles.row}>
          <TextInput
            accessibilityLabel={label}
            value={value}
            onChangeText={onChange}
            onFocus={() => setFocused(true)}
            onBlur={() => setFocused(false)}
            keyboardType="number-pad"
            placeholder="0"
            placeholderTextColor={colors.placeholder}
            editable={editable}
            style={styles.input}
          />
          <Text style={styles.currency}>{currency}</Text>
        </View>
      </View>

      {error != null && <Text style={styles.error}>{error}</Text>}

      {presets && presets.length > 0 && (
        <View style={styles.presets}>
          {presets.map((preset) => {
            const selected = value === String(preset);
            return (
              <Pressable
                key={preset}
                accessibilityRole="button"
                accessibilityState={{ selected }}
                accessibilityLabel={formatAmount(preset, currency)}
                disabled={!editable}
                onPress={() => onChangeText(String(preset))}
                style={[styles.preset, selected && styles.presetSelected, !editable && styles.presetDisabled]}
              >
                <Text style={[styles.presetLabel, selected && styles.presetLabelSelected]}>
                  {formatRounded(preset)}
                </Text>
              </Pressable>
            );
          })}
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  box: {
    backgroundColor: colors.surface,
    borderWidth: 1.5,
    borderColor: colors.border,
    borderRadius: radius.lg,
    paddingHorizontal: spacing.md + 2,
    paddingTop: spacing.md,
    paddingBottom: spacing.sm + 2,
  },
  boxActive: { borderColor: colors.primary },
  boxError: { borderColor: colors.danger },
  boxDisabled: { backgroundColor: colors.surfaceAlt },
  label: {
    fontSize: typography.overline,
    color: colors.muted,
    letterSpacing: 0.6,
    textTransform: 'uppercase',
  },
  row: { flexDirection: 'row', alignItems: 'baseline', gap: spacing.sm },
  input: {
    flex: 1,
    // Sans `minWidth: 0`, un champ en flex:1 refuse de descendre sous sa
    // largeur intrinsèque et repousse le suffixe « HTG » hors de la carte.
    minWidth: 0,
    fontSize: typography.amount,
    fontWeight: '700',
    letterSpacing: -0.6,
    color: colors.text,
    paddingVertical: spacing.xs,
  },
  currency: { fontSize: typography.body, fontWeight: '600', color: colors.muted },
  error: { fontSize: typography.overline, color: colors.danger, marginTop: spacing.xs },
  presets: { flexDirection: 'row', gap: spacing.sm, marginTop: spacing.sm + 2 },
  preset: {
    flex: 1,
    minHeight: 44,
    borderRadius: radius.sm + 2,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.xs,
  },
  presetSelected: { backgroundColor: colors.accentSoft, borderColor: '#E4CFA6' },
  presetDisabled: { opacity: 0.5 },
  presetLabel: { fontSize: typography.label - 1, fontWeight: '600', color: colors.text },
  presetLabelSelected: { color: colors.accentText },
});
