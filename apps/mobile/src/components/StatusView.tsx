import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, fonts, radius, spacing, typography } from '../theme';
import { Icon, IconName } from './Icon';

export type StatusTone = 'success' | 'waiting' | 'danger';

interface Props {
  tone: StatusTone;
  title: string;
  /** Détail renvoyé par le serveur, ou consigne à suivre. */
  message?: string;
  /** Bouton d'action, quand il y a quelque chose à faire. */
  action?: React.ReactNode;
}

const TONES: Record<StatusTone, { icon: IconName; color: string; bg: string }> = {
  success: { icon: 'check', color: colors.success, bg: colors.successSoft },
  waiting: { icon: 'clock', color: colors.accentInk, bg: colors.warningSoft },
  danger: { icon: 'alert', color: colors.danger, bg: colors.dangerSoft },
};

/**
 * Écran de résultat : réussi, en cours, échoué — les trois états exigés
 * par le brief, et rien entre les deux.
 *
 * « En cours » a délibérément sa propre teinte et sa propre icône plutôt
 * que d'emprunter celles du succès : c'est exactement la confusion qui
 * pousse un client à repartir en croyant son argent arrivé.
 */
export function StatusView({ tone, title, message, action }: Props) {
  const palette = TONES[tone];
  return (
    <View style={styles.container}>
      <View style={[styles.badge, { backgroundColor: palette.bg }]}>
        <Icon name={palette.icon} size={34} color={palette.color} />
      </View>
      <Text style={[styles.title, { color: tone === 'success' ? colors.success : colors.text }]}>{title}</Text>
      {message && <Text style={styles.message}>{message}</Text>}
      {action && <View style={styles.action}>{action}</View>}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, alignItems: 'center', justifyContent: 'center', paddingHorizontal: spacing.sm },
  badge: {
    width: 84,
    height: 84,
    borderRadius: radius.xl,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.lg,
  },
  title: {
    fontSize: typography.title,
    fontFamily: fonts.bold,
    textAlign: 'center',
    letterSpacing: -0.3,
    lineHeight: 31,
  },
  message: {
    fontSize: typography.label, fontFamily: fonts.regular,
    color: colors.muted,
    textAlign: 'center',
    lineHeight: 22,
    marginTop: spacing.sm + 2,
  },
  action: { alignSelf: 'stretch', marginTop: spacing.lg },
});
