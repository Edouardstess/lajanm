import React, { useEffect, useState } from 'react';
import { StyleSheet, Switch, Text, View } from 'react-native';
import { ApiError } from '../api/client';
import { Card } from '../components/Card';
import { Chip } from '../components/Chip';
import { Field } from '../components/Field';
import { Icon } from '../components/Icon';
import { InfoNote } from '../components/InfoNote';
import { PrimaryButton } from '../components/PrimaryButton';
import { Screen } from '../components/Screen';
import { SectionHeader } from '../components/SectionHeader';
import { useAuth } from '../context/AuthContext';
import { SUPPORTED_LOCALES, useTranslation } from '../i18n';
import { isBiometricLockEnabled, setBiometricLockEnabled } from '../security/biometricPreference';
import { colors, radius, spacing, typography } from '../theme';

// Endonyms, deliberately untranslated: someone looking for their own
// language finds it faster by its own name than by its name in a language
// they may not read.
const LOCALE_LABELS: Record<string, string> = {
  ht: 'Kreyòl',
  fr: 'Français',
  en: 'English',
};

export function ProfileScreen({ navigation }: { navigation: { navigate: (screen: string) => void } }) {
  const { t, locale, setLocale } = useTranslation();
  const { user, refreshProfile, logout } = useAuth();
  const [fullName, setFullName] = useState(user?.fullName ?? '');
  const [email, setEmail] = useState(user?.email ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [biometricEnabled, setBiometricEnabledState] = useState(false);

  useEffect(() => {
    isBiometricLockEnabled().then(setBiometricEnabledState);
  }, []);

  if (!user) return null;

  const verified = user.tier === 'verified';

  const onSave = async () => {
    setSaving(true);
    setError(null);
    try {
      await refreshProfile({ fullName, email });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('common.error_generic'));
    } finally {
      setSaving(false);
    }
  };

  const onToggleBiometric = async (value: boolean) => {
    setBiometricEnabledState(value);
    await setBiometricLockEnabled(value);
  };

  return (
    <Screen scroll>
      {/* L'identité et le niveau de compte d'abord : c'est ce qui décide
          des plafonds, donc la première chose à vérifier ici. */}
      <Card style={styles.identity}>
        <View style={[styles.badge, verified && styles.badgeVerified]}>
          <Icon name={verified ? 'check' : 'person'} size={22} color={verified ? colors.success : colors.muted} />
        </View>
        <View style={styles.identityBody}>
          <Text style={styles.phone}>{user.phone}</Text>
          <Text style={[styles.tier, verified && styles.tierVerified]}>
            {verified ? t('profile.tier_verified') : t('profile.tier_basic')}
          </Text>
        </View>
      </Card>

      {!verified && (
        <View style={styles.upsell}>
          <InfoNote>{t('kyc.explanation')}</InfoNote>
          <PrimaryButton
            variant="secondary"
            label={t('kyc.title')}
            onPress={() => navigation.navigate('Kyc')}
          />
        </View>
      )}

      <SectionHeader title={t('profile.details_title')} />
      <Field label={t('profile.full_name_label')} value={fullName} onChangeText={setFullName} />
      <Field
        label={t('profile.email_label')}
        value={email}
        onChangeText={setEmail}
        keyboardType="email-address"
        autoCapitalize="none"
      />
      {error && <InfoNote tone="danger">{error}</InfoNote>}
      <PrimaryButton label={t('common.save')} onPress={onSave} loading={saving} />

      <SectionHeader title={t('profile.security_title')} />
      <Card flat style={styles.toggleRow}>
        <Text style={styles.toggleLabel}>{t('security.biometric_toggle')}</Text>
        <Switch
          value={biometricEnabled}
          onValueChange={onToggleBiometric}
          trackColor={{ true: colors.primary, false: colors.borderStrong }}
          thumbColor={colors.surface}
        />
      </Card>
      <PrimaryButton
        variant="quiet"
        icon="lock"
        label={t('profile.devices_title')}
        onPress={() => navigation.navigate('Devices')}
      />

      <SectionHeader title={t('profile.language_label')} />
      <View style={styles.locales}>
        {SUPPORTED_LOCALES.map((option) => (
          <Chip
            key={option}
            label={LOCALE_LABELS[option] ?? option}
            selected={locale === option}
            onPress={() => setLocale(option)}
          />
        ))}
      </View>

      <View style={styles.logout}>
        <PrimaryButton variant="secondary" label={t('profile.logout')} onPress={logout} />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  identity: { flexDirection: 'row', alignItems: 'center', gap: spacing.md - 2 },
  badge: {
    width: 48,
    height: 48,
    borderRadius: radius.sm + 2,
    backgroundColor: colors.surfaceAlt,
    alignItems: 'center',
    justifyContent: 'center',
  },
  badgeVerified: { backgroundColor: colors.successSoft },
  identityBody: { flex: 1 },
  phone: { fontSize: typography.body, fontWeight: '700', color: colors.text },
  tier: { fontSize: typography.caption, color: colors.muted, marginTop: 3 },
  tierVerified: { color: colors.success, fontWeight: '600' },
  upsell: { marginTop: spacing.md, gap: spacing.xs },
  toggleRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
  toggleLabel: { flex: 1, fontSize: typography.label, color: colors.text },
  locales: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm },
  logout: { marginTop: spacing.xl },
});
