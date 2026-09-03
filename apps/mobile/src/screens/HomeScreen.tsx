import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { PrimaryButton } from '../components/PrimaryButton';
import { useAuth } from '../context/AuthContext';
import { useBalance } from '../hooks/useBalance';
import { useTranslation } from '../i18n';
import { colors, spacing, typography } from '../theme';

export function HomeScreen({ navigation }: { navigation: { navigate: (screen: string) => void } }) {
  const { user } = useAuth();
  const { t } = useTranslation();
  const { snapshot, isFromCache, loading } = useBalance();

  return (
    <View style={styles.container}>
      <Text style={styles.subtitle}>{user?.phone}</Text>

      <View style={styles.balanceCard}>
        <Text style={styles.balanceLabel}>{t('wallet.balance_title')}</Text>
        {snapshot ? (
          <>
            <Text style={styles.balanceValue}>
              {(Number(snapshot.balanceMinor) / 100).toFixed(2)} {snapshot.currency}
            </Text>
            {isFromCache && (
              <Text style={styles.stale}>
                {t('wallet.balance_stale')}: {new Date(snapshot.asOf).toLocaleTimeString()}
              </Text>
            )}
          </>
        ) : (
          <Text style={styles.balanceValue}>{loading ? t('common.loading') : '—'}</Text>
        )}
      </View>

      <PrimaryButton label={t('wallet.transfer_title')} onPress={() => navigation.navigate('Transfer')} />
      <PrimaryButton label={t('topup.title')} variant="secondary" onPress={() => navigation.navigate('Topup')} />
      <PrimaryButton
        label={t('wallet.history_title')}
        variant="secondary"
        onPress={() => navigation.navigate('History')}
      />
      <PrimaryButton
        label={t('profile.title')}
        variant="secondary"
        onPress={() => navigation.navigate('Profile')}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, padding: spacing.lg },
  subtitle: { fontSize: typography.body, color: colors.muted, marginBottom: spacing.md },
  balanceCard: {
    backgroundColor: colors.primary,
    borderRadius: 16,
    padding: spacing.lg,
    marginBottom: spacing.lg,
  },
  balanceLabel: { color: colors.primaryText, fontSize: typography.label, opacity: 0.85 },
  balanceValue: { color: colors.primaryText, fontSize: 32, fontWeight: '700', marginTop: spacing.xs },
  stale: { color: colors.primaryText, opacity: 0.75, fontSize: typography.label, marginTop: spacing.sm },
});
