import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, fonts, radius, spacing, typography } from '../theme';
import { Icon, IconName } from './Icon';

interface Props {
  icon?: IconName;
  title: string;
  action?: React.ReactNode;
}

export function EmptyState({ icon = 'clock', title, action }: Props) {
  return (
    <View style={styles.container}>
      <View style={styles.badge}>
        <Icon name={icon} size={26} color={colors.muted} />
      </View>
      <Text style={styles.title}>{title}</Text>
      {action && <View style={styles.action}>{action}</View>}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { alignItems: 'center', paddingTop: spacing.xl, paddingHorizontal: spacing.md },
  badge: {
    width: 60,
    height: 60,
    borderRadius: radius.lg,
    backgroundColor: colors.surfaceAlt,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.md,
  },
  title: { fontSize: typography.label, fontFamily: fonts.regular, color: colors.muted, textAlign: 'center', lineHeight: 22 },
  action: { alignSelf: 'stretch', marginTop: spacing.lg },
});
