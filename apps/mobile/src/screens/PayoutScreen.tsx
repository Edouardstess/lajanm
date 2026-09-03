import * as Crypto from 'expo-crypto';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { ApiError } from '../api/client';
import { getPayoutLimit, initiatePayout } from '../api/payout';
import { PrimaryButton } from '../components/PrimaryButton';
import { useTranslation } from '../i18n';
import { colors, spacing, touchTarget, typography } from '../theme';

type ScreenState = 'form' | 'completed' | 'failed';

export function PayoutScreen() {
  const { t } = useTranslation();
  const [amount, setAmount] = useState('');
  const [maxAmount, setMaxAmount] = useState<number | null>(null);
  const [state, setState] = useState<ScreenState>('form');
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    getPayoutLimit()
      .then((res) => setMaxAmount(res.maxAmountHTG))
      .catch(() => {});
  }, []);

  const onSubmit = async () => {
    const amountHTG = parseInt(amount, 10);
    if (!amountHTG || amountHTG < 1) return;

    setSubmitting(true);
    setMessage(null);
    try {
      const result = await initiatePayout(amountHTG, Crypto.randomUUID());
      if (result.status === 'completed') {
        setState('completed');
      } else {
        setMessage(result.failureReason ?? t('payout.status_failed'));
        setState('failed');
      }
    } catch (err) {
      setMessage(err instanceof ApiError ? err.message : t('common.error_generic'));
      setState('failed');
    } finally {
      setSubmitting(false);
    }
  };

  if (state === 'completed') {
    return (
      <View style={styles.container}>
        <Text style={[styles.title, styles.success]}>{t('payout.status_completed')}</Text>
      </View>
    );
  }

  if (state === 'failed') {
    return (
      <View style={styles.container}>
        <Text style={styles.title}>{t('payout.status_failed_refunded')}</Text>
        {message && <Text style={styles.errorText}>{message}</Text>}
        <PrimaryButton label={t('common.retry')} onPress={() => setState('form')} />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>{t('payout.title')}</Text>
      {maxAmount !== null && (
        <Text style={styles.limitText}>
          {t('payout.limit_label')}: {maxAmount} HTG
        </Text>
      )}

      <Text style={styles.label}>{t('payout.amount_label')}</Text>
      <TextInput
        style={styles.input}
        value={amount}
        onChangeText={setAmount}
        keyboardType="number-pad"
        placeholder="500"
      />

      <PrimaryButton label={t('payout.submit_button')} onPress={onSubmit} loading={submitting} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, padding: spacing.lg, justifyContent: 'center' },
  title: { fontSize: typography.title, fontWeight: '700', color: colors.text, marginBottom: spacing.md, textAlign: 'center' },
  success: { color: colors.success },
  limitText: { fontSize: typography.label, color: colors.muted, marginBottom: spacing.lg, textAlign: 'center' },
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
