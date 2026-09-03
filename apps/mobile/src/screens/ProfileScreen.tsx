import React, { useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { ApiError } from '../api/client';
import { PrimaryButton } from '../components/PrimaryButton';
import { useAuth } from '../context/AuthContext';
import { useTranslation } from '../i18n';
import { colors, spacing, touchTarget, typography } from '../theme';

export function ProfileScreen({ navigation }: { navigation: { navigate: (screen: string) => void } }) {
  const { t } = useTranslation();
  const { user, refreshProfile, logout } = useAuth();
  const [fullName, setFullName] = useState(user?.fullName ?? '');
  const [email, setEmail] = useState(user?.email ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
});
