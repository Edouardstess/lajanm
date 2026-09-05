import * as Crypto from 'expo-crypto';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { ApiError } from '../api/client';
import { getPayoutLimit, initiatePayout } from '../api/payout';
import { AmountField } from '../components/AmountField';
import { Field } from '../components/Field';
import { PrimaryButton } from '../components/PrimaryButton';
import { SafetyNote } from '../components/SafetyNote';
import { Screen } from '../components/Screen';
import { StatusView } from '../components/StatusView';
import { formatRounded } from '../format';
import { useOtpStep } from '../hooks/useOtpStep';
import { useTranslation } from '../i18n';
import { colors, fonts, radius, spacing, typography } from '../theme';

type ScreenState = 'form' | 'completed' | 'failed' | 'unconfirmed';

const PRESETS = [1000, 2500, 5000, 10000];

export function PayoutScreen() {
  const { t } = useTranslation();
  const [amount, setAmount] = useState('');
  const [maxAmount, setMaxAmount] = useState<number | null>(null);
  const [state, setState] = useState<ScreenState>('form');
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const otp = useOtpStep('payout');

  useEffect(() => {
    getPayoutLimit()
      .then((res) => setMaxAmount(res.maxAmountHTG))
      .catch(() => {});
  }, []);

  const amountHTG = parseInt(amount, 10);
  const ready = Number.isFinite(amountHTG) && amountHTG >= 1;
  const overLimit = ready && maxAmount !== null && amountHTG > maxAmount;

  const onSubmit = async () => {
    if (!ready || overLimit) return;

    setSubmitting(true);
    setMessage(null);
    try {
      const result = await initiatePayout(amountHTG, Crypto.randomUUID(), otp.otpPayload);
      if (result.status === 'completed') {
        setState('completed');
      } else {
        setMessage(result.failureReason ?? t('payout.status_failed'));
        setState('failed');
      }
    } catch (err) {
      if (err instanceof ApiError && !otp.needsOtp && otp.isOtpRequiredError(err)) {
        await otp.beginOtpFlow();
      } else if (err instanceof ApiError) {
        // The server answered and rejected the payout, so no money moved.
        setMessage(err.message);
        setState('failed');
      } else {
        // No answer at all (timeout/offline). The payout may well have been
        // accepted and be in flight — claiming it failed, let alone that the
        // money was refunded, would be a lie the user acts on. Say only what
        // we know: unconfirmed, check your history.
        setState('unconfirmed');
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (state === 'completed') {
    return (
      <Screen>
        <StatusView tone="success" title={t('payout.status_completed')} />
      </Screen>
    );
  }

  if (state === 'unconfirmed') {
    return (
      <Screen>
        <StatusView
          tone="waiting"
          title={t('payout.status_unconfirmed')}
          action={<PrimaryButton label={t('common.retry')} onPress={() => setState('form')} />}
        />
      </Screen>
    );
  }

  if (state === 'failed') {
    return (
      <Screen>
        <StatusView
          tone="danger"
          title={t('payout.status_failed_refunded')}
          message={message ?? undefined}
          action={<PrimaryButton label={t('common.retry')} onPress={() => setState('form')} />}
        />
      </Screen>
    );
  }

  const footer = (
    <>
      <PrimaryButton
        icon={otp.needsOtp ? 'lock' : 'arrow-up'}
        label={otp.needsOtp ? t('security.otp_submit') : t('payout.submit_button')}
        onPress={onSubmit}
        loading={submitting || otp.requesting}
        disabled={otp.needsOtp ? otp.otpCode.length < 4 : !ready || overLimit}
      />
      <SafetyNote>{t('payout.safety')}</SafetyNote>
    </>
  );

  return (
    <Screen scroll footer={footer}>
      <AmountField
        label={t('payout.amount_label')}
        value={amount}
        onChangeText={setAmount}
        presets={PRESETS}
        editable={!otp.needsOtp}
        error={overLimit ? t('payout.over_limit') : undefined}
      />

      {otp.needsOtp && (
        <View style={styles.otp}>
          <Field
            label={t('security.otp_label')}
            value={otp.otpCode}
            onChangeText={otp.setOtpCode}
            keyboardType="number-pad"
            maxLength={6}
            placeholder="123456"
          />
        </View>
      )}

      {/*
        Le plafond vient du serveur (Circulaire BRH n°121) et n'est jamais
        codé en dur ici. La jauge le rend tangible avant la saisie plutôt
        qu'après un refus : un rejet qu'on pouvait voir venir est un rejet
        de trop.
      */}
      {maxAmount !== null && (
        <View style={styles.limitCard}>
          <View style={styles.limitHeader}>
            <Text style={styles.limitLabel}>{t('payout.limit_label')}</Text>
            <Text style={styles.limitValue}>{formatRounded(maxAmount, 'HTG')}</Text>
          </View>
          <View style={styles.track}>
            <View
              style={[
                styles.fill,
                { width: `${Math.min(100, ((ready ? amountHTG : 0) / maxAmount) * 100)}%` },
                overLimit && styles.fillOver,
              ]}
            />
          </View>
          <Text style={styles.limitFoot}>
            {t('payout.limit_used')} {formatRounded(ready ? amountHTG : 0)} / {formatRounded(maxAmount, 'HTG')}
          </Text>
        </View>
      )}
    </Screen>
  );
}

const styles = StyleSheet.create({
  otp: { marginTop: spacing.lg },
  limitCard: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    padding: spacing.md - 1,
    marginTop: spacing.lg,
  },
  limitHeader: { flexDirection: 'row', alignItems: 'center', marginBottom: spacing.sm + 2 },
  limitLabel: { flex: 1, fontSize: typography.caption, fontFamily: fonts.regular, color: colors.muted },
  limitValue: { fontSize: typography.caption, fontFamily: fonts.bold, color: colors.text },
  track: { height: 7, borderRadius: radius.pill, backgroundColor: colors.surfaceAlt, overflow: 'hidden' },
  fill: { height: '100%', borderRadius: radius.pill, backgroundColor: colors.primary },
  fillOver: { backgroundColor: colors.danger },
  limitFoot: { fontSize: typography.overline, fontFamily: fonts.regular, color: colors.muted, marginTop: spacing.sm + 1 },
});
