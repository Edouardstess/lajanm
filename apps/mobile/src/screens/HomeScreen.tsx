import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { PrimaryButton } from '../components/PrimaryButton';
import { useAuth } from '../context/AuthContext';
import { useTranslation } from '../i18n';
import { colors, spacing, typography } from '../theme';

/**
 * Placeholder landing screen. Balance/transfer/history land here in the
 * wallet module (Module 3); payout in Module 4.
 */
export function HomeScreen({ navigation }: { navigation: { navigate: (screen: string) => void } }) {
  const { user } = useAuth();
  const { t } = useTranslation();
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Lajan&apos;m</Text>
      <Text style={styles.subtitle}>{user?.phone}</Text>
      <PrimaryButton label={t('topup.title')} onPress={() => navigation.navigate('Topup')} />
      <PrimaryButton
        label={t('profile.title')}
        variant="secondary"
        onPress={() => navigation.navigate('Profile')}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, alignItems: 'center', justifyContent: 'center', padding: spacing.lg },
  title: { fontSize: typography.title, fontWeight: '700', color: colors.text },
  subtitle: { fontSize: typography.body, color: colors.muted, marginTop: spacing.sm, marginBottom: spacing.lg },
});
