import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { formatMinor } from '../format';
import { colors, fonts, radius, spacing, typography } from '../theme';
import { Icon, IconName } from './Icon';

interface Props {
  title: string;
  date: string;
  amountMinor: string | number;
  currency: string;
  direction: 'credit' | 'debit';
  /** Libellé d'état, affiché seulement si la transaction n'est pas finie. */
  statusLabel?: string;
  statusTone?: 'waiting' | 'danger';
}

const ICONS: Record<Props['direction'], IconName> = { credit: 'arrow-down', debit: 'arrow-up' };

/**
 * Une ligne d'historique. Le signe et la couleur disent le sens, mais
 * l'icône aussi : sur une petite dalle, « +2 000 » et « −2 000 » se
 * confondent vite, une flèche entrante ou sortante beaucoup moins.
 */
export function TransactionRow({
  title,
  date,
  amountMinor,
  currency,
  direction,
  statusLabel,
  statusTone = 'waiting',
}: Props) {
  const credit = direction === 'credit';
  const amount = `${credit ? '+' : '−'}${formatMinor(amountMinor)}`;

  return (
    <View style={styles.row} accessible accessibilityLabel={`${title}, ${date}, ${amount} ${currency}`}>
      <View style={[styles.icon, credit ? styles.iconCredit : styles.iconDebit]}>
        <Icon name={ICONS[direction]} size={18} color={credit ? colors.success : colors.primary} />
      </View>

      <View style={styles.body}>
        <Text style={styles.title} numberOfLines={1}>
          {title}
        </Text>
        <Text style={styles.date} numberOfLines={1}>
          {date}
        </Text>
      </View>

      <View style={styles.amountBox}>
        <Text style={[styles.amount, credit && styles.amountCredit]} numberOfLines={1}>
          {amount}
        </Text>
        {statusLabel && (
          <Text style={[styles.status, statusTone === 'danger' && styles.statusDanger]}>{statusLabel}</Text>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md - 3,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    padding: spacing.md - 2,
    marginBottom: spacing.sm,
  },
  icon: { width: 40, height: 40, borderRadius: radius.sm, alignItems: 'center', justifyContent: 'center' },
  // L'or est réservé à l'action principale : il ne sert pas ici à
  // colorer une ligne sur trois de l'historique.
  iconCredit: { backgroundColor: colors.successSoft },
  iconDebit: { backgroundColor: colors.primarySoft },
  body: { flex: 1 },
  title: { fontSize: typography.label - 1, fontFamily: fonts.semibold, color: colors.text },
  date: { fontSize: typography.overline, fontFamily: fonts.regular, color: colors.muted, marginTop: 3 },
  amountBox: { alignItems: 'flex-end' },
  amount: { fontSize: typography.label, fontFamily: fonts.bold, color: colors.text, letterSpacing: -0.2 },
  amountCredit: { color: colors.success },
  status: { fontSize: 11, fontFamily: fonts.semibold, color: colors.accentInk, marginTop: 3 },
  statusDanger: { color: colors.danger },
});
