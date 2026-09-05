import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, radius, spacing, typography } from '../theme';
import { Icon, IconName } from './Icon';

type Tone = 'neutral' | 'waiting' | 'success' | 'danger';

interface Props {
  children: React.ReactNode;
  tone?: Tone;
  icon?: IconName;
}

const TONES: Record<Tone, { bg: string; border: string; icon: string; defaultIcon: IconName }> = {
  neutral: { bg: colors.surfaceAlt, border: colors.border, icon: colors.muted, defaultIcon: 'help' },
  waiting: { bg: colors.warningSoft, border: '#E4CFA6', icon: colors.accent, defaultIcon: 'clock' },
  success: { bg: colors.successSoft, border: '#C7DED3', icon: colors.success, defaultIcon: 'check' },
  danger: { bg: colors.dangerSoft, border: '#EFCFCC', icon: colors.danger, defaultIcon: 'alert' },
};

/**
 * Encart d'explication. La teinte porte le même sens partout dans
 * l'app : or = on attend une confirmation, vert = c'est acquis,
 * rouge = ça a échoué. Le texte le dit aussi — la couleur seule ne
 * suffit jamais (daltonisme, écran en plein soleil).
 */
export function InfoNote({ children, tone = 'neutral', icon }: Props) {
  const palette = TONES[tone];
  return (
    <View style={[styles.note, { backgroundColor: palette.bg, borderColor: palette.border }]}>
      <Icon name={icon ?? palette.defaultIcon} size={17} color={palette.icon} style={styles.icon} />
      <Text style={styles.text}>{children}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  note: {
    flexDirection: 'row',
    gap: spacing.sm + 3,
    borderWidth: 1,
    borderRadius: radius.md,
    padding: spacing.md - 2,
  },
  icon: { marginTop: 1 },
  text: { flex: 1, fontSize: typography.caption, lineHeight: 19, color: colors.text },
});
