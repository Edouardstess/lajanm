import React from 'react';
import { StyleSheet, View, ViewStyle } from 'react-native';
import { colors, elevation, radius, spacing } from '../theme';

interface Props {
  children: React.ReactNode;
  style?: ViewStyle;
  /** `flat` retire l'ombre : pour les cartes empilées d'une liste. */
  flat?: boolean;
}

export function Card({ children, style, flat }: Props) {
  return <View style={[styles.card, !flat && elevation.card, style]}>{children}</View>;
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
  },
});
