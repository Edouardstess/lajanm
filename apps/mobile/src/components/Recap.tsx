import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, fonts, spacing, typography } from '../theme';

export interface RecapLine {
  label: string;
  value: string;
  /** La dernière ligne, celle qui engage : mise en avant. */
  total?: boolean;
}

/**
 * Récapitulatif avant de valider. Il existe pour une raison précise :
 * le montant saisi et le montant réellement débité ne sont pas la même
 * chose dès qu'il y a des frais, et l'utilisateur doit voir les deux
 * avant d'appuyer, pas après.
 */
export function Recap({ lines }: { lines: RecapLine[] }) {
  return (
    <View style={styles.box}>
      {lines.map((line) => (
        <View key={line.label} style={[styles.row, line.total && styles.totalRow]}>
          <Text style={[styles.label, line.total && styles.totalLabel]}>{line.label}</Text>
          <Text style={[styles.value, line.total && styles.totalValue]}>{line.value}</Text>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  box: { borderTopWidth: 1, borderTopColor: colors.border, paddingTop: spacing.md, marginTop: spacing.xs },
  row: { flexDirection: 'row', alignItems: 'center', marginBottom: spacing.sm + 1 },
  totalRow: { marginBottom: 0 },
  label: { flex: 1, fontSize: typography.label - 1, fontFamily: fonts.regular, color: colors.muted },
  totalLabel: { fontSize: typography.label, fontFamily: fonts.semibold, color: colors.text },
  value: { fontSize: typography.label - 1, fontFamily: fonts.semibold, color: colors.text },
  totalValue: { fontSize: typography.body, fontFamily: fonts.bold, color: colors.text },
});
