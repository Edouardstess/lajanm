import React, { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Switch, Text, TextInput, View } from 'react-native';
import { ApiError } from '../api/client';
import { PrimaryButton } from '../components/PrimaryButton';
import { useAuth } from '../context/AuthContext';
import { SUPPORTED_LOCALES, useTranslation } from '../i18n';
import { isBiometricLockEnabled, setBiometricLockEnabled } from '../security/biometricPreference';
import { colors, spacing, touchTarget, typography } from '../theme';

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
    <View style={styles.container}>
      <Text style={styles.title}>{t('profile.title')}</Text>
      <Text style={styles.tier}>
        {user.tier === 'verified' ? t('profile.tier_verified') : t('profile.tier_basic')}
      </Text>

      <Text style={styles.label}>{t('profile.full_name_label')}</Text>
      <TextInput style={styles.input} value={fullName} onChangeText={setFullName} />

      <Text style={styles.label}>{t('profile.email_label')}</Text>
      <TextInput
        style={styles.input}
        value={email}
        onChangeText={setEmail}
        keyboardType="email-address"
        autoCapitalize="none"
      />

      {error && <Text style={styles.error}>{error}</Text>}

      <PrimaryButton label={t('common.save')} onPress={onSave} loading={saving} />

      <View style={styles.toggleRow}>
        <Text style={styles.toggleLabel}>{t('security.biometric_toggle')}</Text>
        <Switch value={biometricEnabled} onValueChange={onToggleBiometric} />
      </View>

      <Text style={styles.label}>{t('profile.language_label')}</Text>
      <View style={styles.localeRow}>
        {SUPPORTED_LOCALES.map((option) => (
          <Pressable
            key={option}
            accessibilityRole="button"
            accessibilityState={{ selected: locale === option }}
            style={[styles.localeChip, locale === option && styles.localeChipSelected]}
            onPress={() => setLocale(option)}
          >
            <Text style={[styles.localeLabel, locale === option && styles.localeLabelSelected]}>
              {LOCALE_LABELS[option] ?? option}
            </Text>
          </Pressable>
        ))}
      </View>

      <PrimaryButton
        label={t('profile.devices_title')}
        variant="secondary"
        onPress={() => navigation.navigate('Devices')}
      />
      <PrimaryButton label={t('profile.logout')} variant="secondary" onPress={logout} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, padding: spacing.lg },
  title: { fontSize: typography.title, fontWeight: '700', color: colors.text },
  tier: { fontSize: typography.label, color: colors.muted, marginBottom: spacing.lg },
  label: { fontSize: typography.label, color: colors.text, marginTop: spacing.md, marginBottom: spacing.xs },
  input: {
    minHeight: touchTarget.minHeight,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    paddingHorizontal: spacing.md,
    fontSize: typography.body,
    color: colors.text,
  },
  error: { color: colors.danger, marginTop: spacing.md },
  toggleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: spacing.md,
    marginTop: spacing.sm,
  },
  toggleLabel: { fontSize: typography.body, color: colors.text, flex: 1, marginRight: spacing.md },
  localeRow: { flexDirection: 'row', flexWrap: 'wrap' },
  localeChip: {
    minHeight: touchTarget.minHeight,
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    paddingHorizontal: spacing.md,
    marginRight: spacing.sm,
    marginBottom: spacing.sm,
  },
  localeChipSelected: { backgroundColor: colors.primary, borderColor: colors.primary },
  localeLabel: { fontSize: typography.body, color: colors.text },
  localeLabelSelected: { color: colors.primaryText, fontWeight: '600' },
});
