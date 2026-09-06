import * as Crypto from 'expo-crypto';
import React, { useState } from 'react';
import { View } from 'react-native';
import { ApiError } from '../api/client';
import { lookupRecipient, RecipientLookup, transfer } from '../api/wallet';
import { AmountField } from '../components/AmountField';
import { Field } from '../components/Field';
import { InfoNote } from '../components/InfoNote';
import { RecipientCard } from '../components/RecipientCard';
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
  const [recipient, setRecipient] = useState<RecipientLookup | null>(null);
  const [checking, setChecking] = useState(false);
  const otp = useOtpStep('transfer');

  const amountHTG = parseInt(amount, 10);
  const amountOk = Number.isFinite(amountHTG) && amountHTG >= 1;
  // On n'envoie que vers un compte confirmé. Un numéro non résolu est
  // très probablement une faute de frappe, et un transfert ne se
  // rattrape pas.
  const ready = amountOk && recipient?.exists === true;

  /**
   * Résout le numéro dès que l'utilisateur quitte le champ, pas à chaque
   * frappe : sur EDGE, une requête par caractère sature le lien et fait
   * clignoter le résultat.
   */
  const checkRecipient = async () => {
    const phone = recipientPhone.trim();
    if (phone.length === 0) {
      setRecipient(null);
      return;
    }
    setChecking(true);
    try {
      setRecipient(await lookupRecipient(phone));
    } catch {
      // Numéro mal formé, limite atteinte, ou réseau absent. On ne bloque
      // pas l'envoi pour autant : l'utilisateur voit qu'on n'a pas pu
      // confirmer, et décide.
      setRecipient(null);
    } finally {
      setChecking(false);
    }
  };

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
        hint={t('wallet.recipient_hint')}
        value={recipientPhone}
        onChangeText={(next) => {
          setRecipientPhone(next);
          // Le résultat précédent ne vaut plus rien dès que le numéro
          // change : le garder afficherait le nom du mauvais destinataire.
          setRecipient(null);
        }}
        onBlur={checkRecipient}
        keyboardType="phone-pad"
        placeholder="+509 34 12 34 56"
        editable={!otp.needsOtp}
      />

      <RecipientCard
        state={checking ? 'checking' : recipient === null ? 'idle' : recipient.exists ? 'found' : 'unknown'}
        displayName={recipient?.displayName ?? null}
        phone={recipient?.phone ?? recipientPhone}
        checkingLabel={t('wallet.recipient_checking')}
        unknownLabel={t('wallet.recipient_unknown')}
        foundLabel={t('wallet.recipient_found')}
        unnamedLabel={t('wallet.recipient_unnamed')}
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
