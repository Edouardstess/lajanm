import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, radius, spacing, typography } from '../theme';
import { Icon, IconName } from './Icon';

interface Props {
  label: string;
  icon: IconName;
  onPress: () => void;
  /** L'action mise en avant : une seule par écran. */
  primary?: boolean;
}

export function QuickAction({ label, icon, onPress, primary }: Props) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      onPress={onPress}
      style={({ pressed }) => [styles.tile, pressed && styles.pressed]}
    >
      <View style={[styles.icon, primary && styles.iconPrimary]}>
        <Icon name={icon} size={19} color={primary ? colors.primaryText : colors.primaryDeep} />
      </View>
      <Text style={styles.label} numberOfLines={2}>
        {label}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  tile: {
    flex: 1,
    minHeight: 92,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.lg - 2,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    paddingVertical: spacing.md - 4,
    paddingHorizontal: spacing.xs + 2,
  },
  pressed: { backgroundColor: colors.surfaceAlt },
  icon: {
    width: 38,
    height: 38,
    borderRadius: radius.sm,
    backgroundColor: colors.accentSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  iconPrimary: { backgroundColor: colors.primary },
  label: { fontSize: typography.overline - 1, fontWeight: '600', color: colors.text, textAlign: 'center', lineHeight: 14 },
});
