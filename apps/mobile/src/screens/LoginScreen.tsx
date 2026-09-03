import React, { useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { PrimaryButton } from '../components/PrimaryButton';
import { useAuth } from '../context/AuthContext';
import { useTranslation } from '../i18n';
import { colors, spacing, touchTarget, typography } from '../theme';
import { ApiError } from '../api/client';

export function LoginScreen({ navigation }: { navigation: { navigate: (screen: string) => void } }) {
  const { t } = useTranslation();
  const { login } = useAuth();
  const [phone, setPhone] = useState('');
  const [pin, setPin] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const onSubmit = async () => {
    setError(null);
    setLoading(true);
    try {
      await login(phone, pin);
    } catch (err) {
      setError(err instanceof ApiError ? t('auth.login_error') : t('common.error_generic'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>{t('auth.login_title')}</Text>

      <Text style={styles.label}>{t('auth.phone_label')}</Text>
      <TextInput
        style={styles.input}
        value={phone}
        onChangeText={setPhone}
        keyboardType="phone-pad"
        placeholder="+509..."
        accessibilityLabel={t('auth.phone_label')}
      />

      <Text style={styles.label}>{t('auth.pin_label')}</Text>
      <TextInput
        style={styles.input}
        value={pin}
        onChangeText={setPin}
        keyboardType="number-pad"
        secureTextEntry
        maxLength={6}
        accessibilityLabel={t('auth.pin_label')}
      />

      {error && <Text style={styles.error}>{error}</Text>}

      <PrimaryButton label={t('auth.login_button')} onPress={onSubmit} loading={loading} />

      <Pressable onPress={() => navigation.navigate('Register')} style={styles.link}>
        <Text style={styles.linkText}>{t('auth.need_account')}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, padding: spacing.lg, justifyContent: 'center' },
  title: { fontSize: typography.title, fontWeight: '700', color: colors.text, marginBottom: spacing.lg },
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
  link: { marginTop: spacing.lg, alignItems: 'center' },
  linkText: { color: colors.primary, fontSize: typography.label },
});
