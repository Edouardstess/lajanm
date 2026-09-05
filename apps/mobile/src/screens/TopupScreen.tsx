import React, { useEffect, useRef, useState } from 'react';
import { Linking } from 'react-native';
import { ApiError } from '../api/client';
import { getTopupStatus, initiateTopup } from '../api/topup';
import { AmountField } from '../components/AmountField';
import { InfoNote } from '../components/InfoNote';
import { PrimaryButton } from '../components/PrimaryButton';
import { SafetyNote } from '../components/SafetyNote';
import { Screen } from '../components/Screen';
import { StatusView } from '../components/StatusView';
import { useTranslation } from '../i18n';

type ScreenState = 'form' | 'pending' | 'completed' | 'failed';

const MIN_AMOUNT_HTG = 25;
const PRESETS = [500, 1000, 2000, 5000];

/**
 * Three honest states, per the product requirement: réussi / en cours /
 * échoué. "pending" is shown both while waiting on MonCash's redirect
 * flow AND while our own initiation call is queued for retry — the user
 * never sees "success" before the webhook has actually confirmed it.
 *
 * L'attente a sa propre couleur (l'or) et sa propre icône : c'est le seul
 * moyen qu'un client ne reparte pas en croyant son dépôt acquis.
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

  const amountHTG = parseInt(amount, 10);
  const ready = Number.isFinite(amountHTG) && amountHTG >= MIN_AMOUNT_HTG;

  const onSubmit = async () => {
    setError(null);
    if (!ready) return;

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
      <Screen>
        <StatusView tone="waiting" title={t('topup.status_pending')} />
      </Screen>
    );
  }

  if (screenState === 'completed') {
    return (
      <Screen>
        <StatusView tone="success" title={t('topup.status_completed')} />
      </Screen>
    );
  }

  if (screenState === 'failed') {
    return (
      <Screen>
        <StatusView
          tone="danger"
          title={t('topup.status_failed')}
          action={
            <PrimaryButton
              label={t('common.retry')}
              onPress={() => {
                setScreenState('form');
                setTransactionId(null);
              }}
            />
          }
        />
      </Screen>
    );
  }

  const footer = (
    <>
      <PrimaryButton
        iconAfter
        icon="arrow-right"
        label={t('topup.submit_button')}
        onPress={onSubmit}
        loading={submitting}
        disabled={!ready}
      />
      <SafetyNote>{t('topup.safety')}</SafetyNote>
    </>
  );

  return (
    <Screen scroll footer={footer}>
      <AmountField
        label={t('topup.amount_label')}
        value={amount}
        onChangeText={setAmount}
        presets={PRESETS}
        error={error ?? undefined}
      />

      <InfoNote tone="waiting">{t('topup.status_pending')}</InfoNote>
    </Screen>
  );
}
