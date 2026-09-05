import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, spacing, typography } from '../theme';
import { Icon } from './Icon';

/**
 * Ligne rassurante sous l'action principale d'un écran d'argent. Elle
 * dit ce qui va se passer ensuite (« on va vous demander un code »,
 * « l'argent ira sur votre compte MonCash ») pour que l'utilisateur
 * appuie en sachant, plutôt qu'en espérant.
 */
export function SafetyNote({ children }: { children: React.ReactNode }) {
  return (
    <View style={styles.row}>
      <Icon name="lock" size={15} color={colors.muted} />
      <Text style={styles.text}>{children}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.xs + 2,
    marginTop: spacing.md - 4,
    paddingHorizontal: spacing.sm,
  },
  text: { flexShrink: 1, fontSize: typography.overline, color: colors.muted, textAlign: 'center' },
});
