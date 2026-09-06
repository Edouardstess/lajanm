import React from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { colors, fonts, radius, spacing, typography } from '../theme';
import { Icon } from './Icon';

type State = 'idle' | 'checking' | 'found' | 'unknown';

interface Props {
  state: State;
  displayName: string | null;
  phone: string;
  checkingLabel: string;
  foundLabel: string;
  unknownLabel: string;
  /** Affiché quand le compte existe mais n'a pas renseigné de nom. */
  unnamedLabel: string;
}

/**
 * Qui va recevoir l'argent, affiché avant d'appuyer sur « Voye ».
 *
 * Un transfert est irréversible. Sans cette carte, l'utilisateur tape
 * huit chiffres et valide à l'aveugle ; un seul chiffre erroné envoie
 * l'argent chez quelqu'un d'autre, et il ne l'apprend qu'après. Voir le
 * nom du destinataire est le seul moment où l'erreur reste rattrapable.
 *
 * L'état `unknown` n'est pas traité comme une erreur de saisie mais comme
 * une information : le numéro peut être juste et la personne ne pas avoir
 * encore de compte Lajan'm.
 */
export function RecipientCard({
  state,
  displayName,
  phone,
  checkingLabel,
  foundLabel,
  unknownLabel,
  unnamedLabel,
}: Props) {
  if (state === 'idle') return null;

  if (state === 'checking') {
    return (
      <View style={[styles.card, styles.neutral]}>
        <ActivityIndicator color={colors.muted} />
        <Text style={styles.checking}>{checkingLabel}</Text>
      </View>
    );
  }

  if (state === 'unknown') {
    return (
      <View style={[styles.card, styles.warning]} accessible accessibilityLabel={unknownLabel}>
        <View style={[styles.badge, styles.badgeWarning]}>
          <Icon name="alert" size={20} color={colors.accentInk} />
        </View>
        <View style={styles.body}>
          <Text style={styles.title}>{unknownLabel}</Text>
          <Text style={styles.phone}>{phone}</Text>
        </View>
      </View>
    );
  }

  const name = displayName ?? unnamedLabel;
  return (
    <View
      style={[styles.card, styles.success]}
      accessible
      accessibilityLabel={`${foundLabel}: ${name}, ${phone}`}
    >
      <View style={[styles.badge, styles.badgeSuccess]}>
        <Icon name="person" size={20} color={colors.success} />
      </View>
      <View style={styles.body}>
        <Text style={styles.label}>{foundLabel}</Text>
        <Text style={styles.name} numberOfLines={1}>
          {name}
        </Text>
        <Text style={styles.phone}>{phone}</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md - 3,
    borderWidth: 1,
    borderRadius: radius.md,
    padding: spacing.md - 2,
    marginTop: -spacing.xs,
    marginBottom: spacing.md,
    minHeight: 64,
  },
  neutral: { backgroundColor: colors.surfaceAlt, borderColor: colors.border },
  success: { backgroundColor: colors.successSoft, borderColor: '#C6E0D0' },
  warning: { backgroundColor: colors.warningSoft, borderColor: '#E8D9A8' },
  badge: {
    width: 40,
    height: 40,
    borderRadius: radius.sm,
    alignItems: 'center',
    justifyContent: 'center',
  },
  badgeSuccess: { backgroundColor: colors.surface },
  badgeWarning: { backgroundColor: colors.surface },
  body: { flex: 1 },
  label: { fontSize: typography.overline, fontFamily: fonts.regular, color: colors.muted },
  name: { fontSize: typography.body - 1, fontFamily: fonts.bold, color: colors.text, marginTop: 2 },
  title: { fontSize: typography.label - 1, fontFamily: fonts.semibold, color: colors.text },
  phone: { fontSize: typography.overline, fontFamily: fonts.regular, color: colors.muted, marginTop: 2 },
  checking: { fontSize: typography.label - 1, fontFamily: fonts.regular, color: colors.muted },
});
