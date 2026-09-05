import React, { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { ApiError } from '../api/client';
import { Field } from '../components/Field';
import { InfoNote } from '../components/InfoNote';
import { Logo } from '../components/Logo';
import { PrimaryButton } from '../components/PrimaryButton';
import { Screen } from '../components/Screen';
import { useAuth } from '../context/AuthContext';
import { useTranslation } from '../i18n';
import { colors, fonts, spacing, typography } from '../theme';

export function LoginScreen({ navigation }: { navigation: { navigate: (screen: string) => void } }) {
  const { t } = useTranslation();
  const { login } = useAuth();
  const [phone, setPhone] = useState('');
  const [pin, setPin] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const ready = phone.trim().length > 0 && pin.length >= 4;

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
    <Screen scroll center>
      <View style={styles.brand}>
        <Logo size="lg" />
      </View>

      <Text style={styles.title}>{t('auth.login_title')}</Text>

      <Field
        label={t('auth.phone_label')}
        value={phone}
        onChangeText={setPhone}
        keyboardType="phone-pad"
        placeholder="+509..."
        autoComplete="tel"
      />

      <Field
        label={t('auth.pin_label')}
        value={pin}
        onChangeText={setPin}
        keyboardType="number-pad"
        secureTextEntry
        maxLength={6}
      />

      {error && <InfoNote tone="danger">{error}</InfoNote>}

      <PrimaryButton
        label={t('auth.login_button')}
        onPress={onSubmit}
        loading={loading}
        disabled={!ready}
      />

      <Pressable onPress={() => navigation.navigate('Register')} style={styles.link} hitSlop={8}>
        <Text style={styles.linkText}>{t('auth.need_account')}</Text>
      </Pressable>
    </Screen>
  );
}

const styles = StyleSheet.create({
  brand: { alignItems: 'center', marginBottom: spacing.xl },
  title: {
    fontSize: typography.title,
    fontFamily: fonts.bold,
    color: colors.text,
    marginBottom: spacing.lg,
    letterSpacing: -0.3,
  },
  link: { marginTop: spacing.lg, alignItems: 'center', minHeight: 44, justifyContent: 'center' },
  linkText: { color: colors.primary, fontSize: typography.label, fontFamily: fonts.semibold },
});
