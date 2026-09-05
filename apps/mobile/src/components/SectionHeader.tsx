import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, spacing, touchTarget, typography } from '../theme';

interface Props {
  title: string;
  actionLabel?: string;
  onAction?: () => void;
}

export function SectionHeader({ title, actionLabel, onAction }: Props) {
  return (
    <View style={styles.row}>
      <Text style={styles.title}>{title}</Text>
      {actionLabel && onAction && (
        <Pressable accessibilityRole="button" onPress={onAction} style={styles.action} hitSlop={12}>
          <Text style={styles.actionLabel}>{actionLabel}</Text>
        </Pressable>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: spacing.lg,
    marginBottom: spacing.sm,
  },
  title: { fontSize: typography.heading, fontWeight: '700', color: colors.text, letterSpacing: -0.2 },
  action: { marginLeft: 'auto', minHeight: touchTarget.minHeight - 20, justifyContent: 'center' },
  actionLabel: { fontSize: typography.caption, fontWeight: '600', color: colors.primary },
});
