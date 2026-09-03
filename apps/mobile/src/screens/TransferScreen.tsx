import * as Crypto from 'expo-crypto';
import React, { useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { ApiError } from '../api/client';
import { transfer } from '../api/wallet';
import { PrimaryButton } from '../components/PrimaryButton';
import { useOtpStep } from '../hooks/useOtpStep';
import { useTranslation } from '../i18n';
import { colors, spacing, touchTarget, typography } from '../theme';

type ScreenState = 'form' | 'sent' | 'offline' | 'error';

export function TransferScreen() {
  const { t } = useTranslation();
  const [recipientPhone, setRecipientPhone] = useState('');
  const [amount, setAmount] = useState('');
  const [state, setState] = useState<ScreenState>('form');
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const otp = useOtpStep('transfer');

  const onSubmit = async () => {
    const amountHTG = parseInt(amount, 10);
    if (!amountHTG || amountHTG < 1 || !recipientPhone) return;

    setSubmitting(true);
    const clientRequestId = Crypto.randomUUID();
    try {
      await transfer(recipientPhone, amountHTG, clientRequestId, otp.otpPayload);
      setState('sent');
    } catch (err) {
      if (err instanceof ApiError) {
        if (!otp.needsOtp && otp.isOtpRequiredError(err)) {
          // Don't show this as a failure — it's the server telling us to
          // collect a code, the transfer hasn't been attempted for real
          // yet in a way that matters to the user.
          await otp.beginOtpFlow();
        } else {
          // The server responded — this is a real outcome (insufficient
          // funds, unknown recipient, wrong OTP...), not a connectivity
          // problem.
          setMessage(err.message);
          setState('error');
        }
      } else {
        // No response at all: we genuinely don't know whether this
        // reached the server. Never say "sent" here — the honest state is
        // "we don't know yet, not confirmed".
        setState('offline');
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (state === 'sent') {
    return (
      <View style={styles.container}>
        <Text style={[styles.title, styles.success]}>{t('wallet.transfer_sent')}</Text>
      </View>
    );
  }

  if (state === 'offline') {
    return (
      <View style={styles.container}>
        <Text style={styles.title}>{t('wallet.transfer_offline')}</Text>
        <PrimaryButton label={t('common.retry')} onPress={() => setState('form')} />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>{t('wallet.transfer_title')}</Text>

      <Text style={styles.label}>{t('wallet.recipient_label')}</Text>
      <TextInput
        style={styles.input}
        value={recipientPhone}
        onChangeText={setRecipientPhone}
        keyboardType="phone-pad"
        placeholder="+509..."
        editable={!otp.needsOtp}
      />

      <Text style={styles.label}>{t('wallet.amount_label')}</Text>
      <TextInput
        style={styles.input}
        value={amount}
        onChangeText={setAmount}
        keyboardType="number-pad"
        placeholder="100"
        editable={!otp.needsOtp}
      />

      {otp.needsOtp && (
        <>
          <Text style={styles.label}>{t('security.otp_label')}</Text>
          <TextInput
            style={styles.input}
            value={otp.otpCode}
            onChangeText={otp.setOtpCode}
            keyboardType="number-pad"
            maxLength={6}
            placeholder="123456"
          />
        </>
      )}

      {state === 'error' && message && <Text style={styles.errorText}>{message}</Text>}

      <PrimaryButton
        label={otp.needsOtp ? t('security.otp_submit') : t('wallet.transfer_submit')}
        onPress={onSubmit}
        loading={submitting || otp.requesting}
        disabled={otp.needsOtp && otp.otpCode.length < 4}
      />
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
