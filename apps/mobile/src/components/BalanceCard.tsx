import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { splitAmount } from '../format';
import { colors, fonts, radius, spacing, typography } from '../theme';
import { Icon } from './Icon';

interface Props {
  label: string;
  balanceMinor?: string | number;
  currency?: string;
  /** Libellé déjà traduit du niveau de compte (« Kont verifye »). */
  tierLabel?: string;
  tierVerified?: boolean;
  /** Rempli seulement quand la valeur vient du cache hors ligne. */
  staleLabel?: string;
  loadingLabel?: string;
}

/**
 * La seule surface sombre de l'application, et le seul chiffre en très
 * grand : le solde est ce que l'utilisateur vient vérifier, tout le
 * reste de l'écran lui est subordonné.
 *
 * Quand la valeur vient du cache, la date de dernière mise à jour est
 * affichée dans la carte elle-même plutôt qu'en note de bas d'écran :
 * un solde périmé lu comme un solde à jour, c'est un virement refusé
 * que l'utilisateur ne comprend pas.
 */
export function BalanceCard({
  label,
  balanceMinor,
  currency = 'HTG',
  tierLabel,
  tierVerified,
  staleLabel,
  loadingLabel,
}: Props) {
  const known = balanceMinor !== undefined && balanceMinor !== null;
  const amount = known ? splitAmount(balanceMinor) : null;

  return (
    <View style={styles.card}>
      {tierLabel && (
        <View style={styles.tier}>
          {tierVerified && <Icon name="check" size={12} color={colors.accentSoft} />}
          <Text style={styles.tierLabel}>{tierLabel}</Text>
        </View>
      )}

      <Text style={styles.label}>{label}</Text>

      {amount ? (
        <Text
          style={styles.value}
          accessibilityLabel={`${label}: ${amount.whole},${amount.decimals} ${currency}`}
          allowFontScaling
          numberOfLines={1}
          adjustsFontSizeToFit
        >
          {amount.whole}
          <Text style={styles.decimals}>,{amount.decimals}</Text>
          <Text style={styles.currency}> {currency}</Text>
        </Text>
      ) : (
        <Text style={styles.value}>{loadingLabel ?? '—'}</Text>
      )}

      {staleLabel && (
        <View style={styles.stale}>
          <Icon name="clock" size={13} color={colors.primaryText} />
          <Text style={styles.staleLabel}>{staleLabel}</Text>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    // Le bleu de la charte tel quel : `primaryDeep` vire au noir et
    // perd l'identité de la marque sur la plus grande surface colorée
    // de l'application.
    backgroundColor: colors.primary,
    borderRadius: radius.xl,
    padding: spacing.lg,
    overflow: 'hidden',
  },
  tier: {
    position: 'absolute',
    top: spacing.lg,
    right: spacing.lg,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    backgroundColor: 'rgba(255, 255, 255, 0.14)',
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.22)',
    borderRadius: radius.pill,
    paddingHorizontal: 11,
    paddingVertical: 6,
  },
  tierLabel: { color: colors.primaryText, fontSize: 11, fontFamily: fonts.semibold },
  label: {
    color: colors.primaryText,
    fontSize: typography.caption, fontFamily: fonts.regular,
    letterSpacing: 0.8,
    textTransform: 'uppercase',
    opacity: 0.72,
  },
  value: {
    color: colors.primaryText,
    fontSize: typography.display,
    fontFamily: fonts.bold,
    letterSpacing: -0.8,
    marginTop: spacing.sm,
  },
  decimals: { fontSize: typography.heading, fontFamily: fonts.bold, letterSpacing: 0 },
  currency: { fontSize: typography.body, fontFamily: fonts.semibold, opacity: 0.8, letterSpacing: 0 },
  stale: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, marginTop: spacing.md },
  staleLabel: { color: colors.primaryText, fontSize: typography.overline, fontFamily: fonts.regular, opacity: 0.75 },
});
