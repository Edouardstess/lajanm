import * as Crypto from 'expo-crypto';
import React, { useState } from 'react';
import { View } from 'react-native';
import { ApiError } from '../api/client';
import { transfer } from '../api/wallet';
import { AmountField } from '../components/AmountField';
import { Field } from '../components/Field';
import { PrimaryButton } from '../components/PrimaryButton';
import { Recap } from '../components/Recap';
import { SafetyNote } from '../components/SafetyNote';
import { Screen } from '../components/Screen';
import { StatusView } from '../components/StatusView';
import { formatAmount, formatMinor } from '../format';
import { useBalance } from '../hooks/useBalance';
import { useOtpStep } from '../hooks/useOtpStep';
import { useTranslation } from '../i18n';

type ScreenState = 'form' | 'sent' | 'offline' | 'error';

const PRESETS = [100, 250, 500, 1000];

export function TransferScreen() {
  const { t } = useTranslation();
  const { snapshot } = useBalance();
  const [recipientPhone, setRecipientPhone] = useState('');
  const [amount, setAmount] = useState('');
  const [state, setState] = useState<ScreenState>('form');
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const otp = useOtpStep('transfer');

  const amountHTG = parseInt(amount, 10);
  const ready = Number.isFinite(amountHTG) && amountHTG >= 1 && recipientPhone.trim().length > 0;

  const onSubmit = async () => {
    if (!ready) return;

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
      <Screen>
        <StatusView tone="success" title={t('wallet.transfer_sent')} />
      </Screen>
    );
  }

  if (state === 'offline') {
    return (
      <Screen>
        <StatusView
          tone="waiting"
          title={t('wallet.transfer_offline')}
          action={<PrimaryButton label={t('common.retry')} onPress={() => setState('form')} />}
        />
      </Screen>
    );
  }

  const footer = (
    <>
      <PrimaryButton
        icon={otp.needsOtp ? 'lock' : 'send'}
        label={otp.needsOtp ? t('security.otp_submit') : t('wallet.transfer_submit')}
        onPress={onSubmit}
        loading={submitting || otp.requesting}
        disabled={otp.needsOtp ? otp.otpCode.length < 4 : !ready}
      />
      <SafetyNote>{t('wallet.transfer_safety')}</SafetyNote>
    </>
  );

  return (
    <Screen scroll footer={footer}>
      <Field
        label={t('wallet.recipient_label')}
        value={recipientPhone}
        onChangeText={setRecipientPhone}
        keyboardType="phone-pad"
        placeholder="+509..."
        editable={!otp.needsOtp}
      />

      <AmountField
        label={t('wallet.amount_label')}
        value={amount}
        onChangeText={setAmount}
        presets={PRESETS}
        editable={!otp.needsOtp}
        error={state === 'error' && message ? message : undefined}
      />

      {otp.needsOtp && (
        <View style={{ marginTop: 24 }}>
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

      {ready && (
        <Recap
          lines={[
            {
              label: t('wallet.balance_title'),
              value: snapshot ? formatMinor(snapshot.balanceMinor, snapshot.currency) : '—',
            },
            {
              // Libellé sans l'unité : elle est déjà dans la valeur, et
              // « Montan an (HTG) — 750,00 HTG » se lit deux fois.
              label: t('wallet.amount_short'),
              value: formatAmount(amountHTG, snapshot?.currency ?? 'HTG'),
              total: true,
            },
          ]}
        />
      )}

    </Screen>
  );
}
