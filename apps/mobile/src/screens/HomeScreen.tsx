import React, { useCallback, useEffect, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { getHistory, WalletHistoryEntry } from '../api/wallet';
import { formatDateTime } from '../format';
import { BalanceCard } from '../components/BalanceCard';
import { EmptyState } from '../components/EmptyState';
import { Icon } from '../components/Icon';
import { PrimaryButton } from '../components/PrimaryButton';
import { QuickAction } from '../components/QuickAction';
import { SectionHeader } from '../components/SectionHeader';
import { TransactionRow } from '../components/TransactionRow';
import { useAuth } from '../context/AuthContext';
import { useBalance } from '../hooks/useBalance';
import { useTranslation } from '../i18n';
import { colors, radius, spacing, typography } from '../theme';

const RECENT_COUNT = 3;

/**
 * L'accueil répond à une seule question — « combien j'ai ? » — et met
 * l'action la plus fréquente à portée de pouce.
 *
 * L'écran d'origine empilait six boutons de même poids : rien n'y
 * indiquait quoi faire en premier, et le solde partageait la vedette
 * avec eux. Ici le solde occupe le haut, quatre raccourcis couvrent
 * l'essentiel, et les trois dernières opérations servent de preuve que
 * l'argent a bien bougé — c'est la question posée juste après le solde.
 */
export function HomeScreen({ navigation }: { navigation: { navigate: (screen: string) => void } }) {
  const { user } = useAuth();
  const { t } = useTranslation();
  const { snapshot, isFromCache, loading, refresh } = useBalance();
  // L'accueil n'a pas d'en-tête de navigation : sans cette marge, son
  // propre en-tête passe sous la barre d'état et l'encoche.
  const insets = useSafeAreaInsets();
  const [recent, setRecent] = useState<WalletHistoryEntry[] | null>(null);

  const loadRecent = useCallback(async () => {
    try {
      setRecent(await getHistory({ limit: RECENT_COUNT }));
    } catch {
      // Réseau absent ou API muette : l'accueil garde son intérêt
      // principal (le solde, servi par le cache). On n'affiche pas
      // d'erreur pour une section secondaire — la section disparaît.
    }
  }, []);

  useEffect(() => {
    loadRecent();
  }, [loadRecent]);

  const onRefresh = useCallback(() => {
    refresh();
    loadRecent();
  }, [refresh, loadRecent]);

  const staleLabel =
    isFromCache && snapshot
      ? `${t('wallet.balance_stale')} · ${formatDateTime(snapshot.asOf)}`
      : undefined;

  return (
    <View style={styles.root}>
      <View style={[styles.header, { paddingTop: insets.top + spacing.md }]}>
        <View style={styles.avatar}>
          {/* Deux lettres si le nom est connu ; sinon une silhouette,
              jamais deux chiffres arrachés au numéro de téléphone, qui ne
              veulent rien dire pour la personne qui les lit. */}
          {monogram(user?.fullName) ? (
            <Text style={styles.avatarText}>{monogram(user?.fullName)}</Text>
          ) : (
            <Icon name="person" size={22} color={colors.primaryDeep} />
          )}
        </View>
        <View style={styles.identity}>
          <Text style={styles.greeting}>{t('wallet.greeting')}</Text>
          <Text style={styles.phone} numberOfLines={1}>
            {user?.fullName?.trim() || user?.phone}
          </Text>
        </View>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={t('support.title')}
          onPress={() => navigation.navigate('Support')}
          style={styles.headerButton}
        >
          <Icon name="help" size={20} color={colors.text} />
        </Pressable>
      </View>

      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={
          <RefreshControl refreshing={loading} onRefresh={onRefresh} tintColor={colors.primary} />
        }
      >
        <BalanceCard
          label={t('wallet.balance_title')}
          balanceMinor={snapshot?.balanceMinor}
          currency={snapshot?.currency}
          tierLabel={user?.tier === 'verified' ? t('profile.tier_verified') : t('profile.tier_basic')}
          tierVerified={user?.tier === 'verified'}
          staleLabel={staleLabel}
          loadingLabel={loading ? t('common.loading') : undefined}
        />

        <View style={styles.actions}>
          <QuickAction
            primary
            icon="send"
            label={t('wallet.transfer_title')}
            onPress={() => navigation.navigate('Transfer')}
          />
          <QuickAction icon="arrow-down" label={t('topup.title')} onPress={() => navigation.navigate('Topup')} />
          <QuickAction icon="arrow-up" label={t('payout.title')} onPress={() => navigation.navigate('Payout')} />
          {/* Libellé court : « Istorik tranzaksyon » ne tient pas dans une
              tuile d'un quart de largeur et s'y coupait en « Istorik
              tranzaksy… ». */}
          <QuickAction icon="clock" label={t('wallet.history_short')} onPress={() => navigation.navigate('History')} />
        </View>

        <SectionHeader
          title={t('wallet.history_title')}
          actionLabel={recent && recent.length > 0 ? t('wallet.see_all') : undefined}
          onAction={() => navigation.navigate('History')}
        />

        {recent === null ? null : recent.length === 0 ? (
          <EmptyState title={t('wallet.history_empty')} />
        ) : (
          recent.map((entry) => (
            <TransactionRow
              key={entry.id}
              title={t(`wallet.type_${entry.operationType}`)}
              date={formatDateTime(entry.createdAt)}
              amountMinor={entry.amountMinor}
              currency={entry.currency}
              direction={entry.direction}
            />
          ))
        )}

        <PrimaryButton
          variant="quiet"
          icon="person"
          label={t('profile.title')}
          onPress={() => navigation.navigate('Profile')}
        />
      </ScrollView>
    </View>
  );
}

/**
 * Les initiales du nom, quand il est connu. Rien sinon : l'appelant
 * affiche alors une silhouette.
 */
function monogram(fullName?: string | null): string | null {
  const parts = (fullName ?? '').trim().split(/\s+/).filter(Boolean);
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return null;
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.ground },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md - 4,
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.md,
    paddingBottom: spacing.md - 2,
  },
  avatar: {
    width: 44,
    height: 44,
    borderRadius: radius.sm + 2,
    backgroundColor: colors.accentSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: { fontSize: typography.body - 1, fontWeight: '700', color: colors.primaryDeep },
  identity: { flex: 1 },
  greeting: { fontSize: typography.caption, color: colors.muted },
  phone: { fontSize: typography.body - 1, fontWeight: '600', color: colors.text, marginTop: 2 },
  headerButton: {
    width: 44,
    height: 44,
    borderRadius: radius.sm + 2,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
    justifyContent: 'center',
  },
  content: { paddingHorizontal: spacing.lg, paddingBottom: spacing.xl },
  actions: { flexDirection: 'row', gap: spacing.sm + 2, marginTop: spacing.lg - 4 },
});
