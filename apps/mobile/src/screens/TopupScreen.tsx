import React, { useEffect, useRef, useState } from 'react';
import { Linking, StyleSheet, Text, TextInput, View } from 'react-native';
import { ApiError } from '../api/client';
import { getTopupStatus, initiateTopup, TopupStatus } from '../api/topup';
import { PrimaryButton } from '../components/PrimaryButton';
import { useTranslation } from '../i18n';
import { colors, spacing, touchTarget, typography } from '../theme';

type ScreenState = 'form' | 'pending' | 'completed' | 'failed';

/**
 * Three honest states, per the product requirement: réussi / en cours /
 * échoué. "pending" is shown both while waiting on MonCash's redirect
 * flow AND while our own initiation call is queued for retry — the user
 * never sees "success" before the webhook has actually confirmed it.
 */
export function TopupScreen() {
  const { t } = useTranslation();
  const [amount, setAmount] = useState('');
  const [screenState, setScreenState] = useState<ScreenState>('form');
  const [transactionId, setTransactionId] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    return () => {
      if (pollRef.current) clearInterval(pollRef.current);
    };
  }, []);

  const pollStatus = (id: string) => {
    pollRef.current = setInterval(async () => {
      try {
        const tx = await getTopupStatus(id);
        if (tx.status === 'completed' || tx.status === 'failed') {
          setScreenState(tx.status as ScreenState);
          if (pollRef.current) clearInterval(pollRef.current);
        }
      } catch {
        // Transient network hiccup while polling — stay on the pending
        // screen and let the next tick retry, rather than surfacing an
        // error for something that isn't the top-up itself failing.
      }
    }, 4000);
  };

  const onSubmit = async () => {
    setError(null);
    const amountHTG = parseInt(amount, 10);
    if (!amountHTG || amountHTG < 25) return;

    setSubmitting(true);
    try {
      const result = await initiateTopup(amountHTG);
      setTransactionId(result.transactionId);
      setScreenState('pending');
      if (result.gatewayUrl) {
        Linking.openURL(result.gatewayUrl).catch(() => {});
      }
      pollStatus(result.transactionId);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('common.error_generic'));
    } finally {
      setSubmitting(false);
    }
  };

  if (screenState === 'pending') {
    return (
      <View style={styles.container}>
        <Text style={styles.title}>{t('topup.status_pending')}</Text>
      </View>
    );
  }

  if (screenState === 'completed') {
    return (
      <View style={styles.container}>
        <Text style={[styles.title, styles.success]}>{t('topup.status_completed')}</Text>
      </View>
    );
  }

  if (screenState === 'failed') {
    return (
      <View style={styles.container}>
        <Text style={[styles.title, styles.errorText]}>{t('topup.status_failed')}</Text>
        <PrimaryButton
          label={t('common.retry')}
          onPress={() => {
            setScreenState('form');
            setTransactionId(null);
          }}
        />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>{t('topup.title')}</Text>
      <Text style={styles.label}>{t('topup.amount_label')}</Text>
      <TextInput
        style={styles.input}
        value={amount}
        onChangeText={setAmount}
        keyboardType="number-pad"
        placeholder="500"
        accessibilityLabel={t('topup.amount_label')}
      />
      {error && <Text style={styles.errorText}>{error}</Text>}
      <PrimaryButton label={t('topup.submit_button')} onPress={onSubmit} loading={submitting} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, padding: spacing.lg, justifyContent: 'center' },
  title: { fontSize: typography.title, fontWeight: '700', color: colors.text, marginBottom: spacing.lg, textAlign: 'center' },
  success: { color: colors.success },
  label: { fontSize: typography.label, color: colors.text, marginBottom: spacing.xs },
  input: {
    minHeight: touchTarget.minHeight,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    paddingHorizontal: spacing.md,
    fontSize: typography.body,
    color: colors.text,
    marginBottom: spacing.md,
  },
  errorText: { color: colors.danger, marginBottom: spacing.md, textAlign: 'center' },
});
