import React from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';
import { colors, radius, spacing, typography } from '../theme';

interface Props {
  label: string;
  selected: boolean;
  onPress: () => void;
}

/** Choix unique dans une courte liste (catégorie, langue). */
export function Chip({ label, selected, onPress }: Props) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected }}
      onPress={onPress}
      style={[styles.chip, selected && styles.selected]}
    >
      <Text style={[styles.label, selected && styles.labelSelected]}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  chip: {
    minHeight: 46,
    justifyContent: 'center',
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.sm,
    paddingHorizontal: spacing.md,
  },
  selected: { backgroundColor: colors.primary, borderColor: colors.primary },
  label: { fontSize: typography.label, color: colors.text },
  labelSelected: { color: colors.primaryText, fontWeight: '600' },
});
