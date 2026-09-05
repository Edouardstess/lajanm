import * as ImagePicker from 'expo-image-picker';
import React, { useState } from 'react';
import { Image, StyleSheet, Text, View } from 'react-native';
import { ApiError } from '../api/client';
import { submitKyc } from '../api/kyc';
import { Icon } from '../components/Icon';
import { InfoNote } from '../components/InfoNote';
import { PrimaryButton } from '../components/PrimaryButton';
import { Screen } from '../components/Screen';
import { StatusView } from '../components/StatusView';
import { useTranslation } from '../i18n';
import { colors, radius, spacing, typography } from '../theme';

/**
 * Captures and submits the two KYC photos. There is no object-storage
 * upload step yet (see SubmitKycDto) — the captured local file URIs are
 * sent as-is. This is a known placeholder, not a finished upload pipeline;
 * wiring real object storage is a follow-up infra task.
 */
export function KycCaptureScreen() {
  const { t } = useTranslation();
  const [idDocumentUri, setIdDocumentUri] = useState<string | null>(null);
  const [selfieUri, setSelfieUri] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [status, setStatus] = useState<'idle' | 'submitted' | 'error'>('idle');
  const [error, setError] = useState<string | null>(null);

  const capture = async (onCaptured: (uri: string) => void) => {
    const permission = await ImagePicker.requestCameraPermissionsAsync();
    if (!permission.granted) return;

    const result = await ImagePicker.launchCameraAsync({ quality: 0.6 });
    if (!result.canceled && result.assets[0]) {
      onCaptured(result.assets[0].uri);
    }
  };

  if (status === 'submitted') {
    return (
      <Screen>
        <StatusView tone="waiting" title={t('kyc.status_pending')} />
      </Screen>
    );
  }

  const onSubmit = async () => {
    if (!idDocumentUri || !selfieUri) return;
    setSubmitting(true);
    setError(null);
    try {
      await submitKyc(idDocumentUri, selfieUri);
      setStatus('submitted');
    } catch (err) {
      setStatus('error');
      setError(err instanceof ApiError ? err.message : t('common.error_generic'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Screen
      scroll
      subtitle={t('kyc.explanation')}
      footer={
        <PrimaryButton
          label={t('kyc.submit_button')}
          onPress={onSubmit}
          loading={submitting}
          disabled={!idDocumentUri || !selfieUri}
        />
      }
    >
      <CaptureSlot
        label={t('kyc.id_document_button')}
        uri={idDocumentUri}
        onPress={() => capture(setIdDocumentUri)}
      />
      <CaptureSlot label={t('kyc.selfie_button')} uri={selfieUri} onPress={() => capture(setSelfieUri)} />

      {error && <InfoNote tone="danger">{error}</InfoNote>}
    </Screen>
  );
}

/**
 * Un emplacement par photo, qui montre la photo prise plutôt que de se
 * contenter de changer la couleur d'un bouton : l'utilisateur doit voir
 * ce qu'il envoie, une pièce d'identité floue étant la première cause de
 * refus de vérification.
 */
function CaptureSlot({ label, uri, onPress }: { label: string; uri: string | null; onPress: () => void }) {
  return (
    <View style={styles.slot}>
      {uri ? (
        <Image source={{ uri }} style={styles.preview} accessibilityIgnoresInvertColors />
      ) : (
        <View style={styles.placeholder}>
          <Icon name="card" size={28} color={colors.placeholder} />
          <Text style={styles.placeholderLabel}>{label}</Text>
        </View>
      )}
      <PrimaryButton label={label} variant={uri ? 'quiet' : 'secondary'} onPress={onPress} />
    </View>
  );
}

const styles = StyleSheet.create({
  slot: { marginBottom: spacing.lg },
  preview: {
    width: '100%',
    height: 180,
    borderRadius: radius.md,
    backgroundColor: colors.surfaceAlt,
  },
  placeholder: {
    height: 180,
    borderRadius: radius.md,
    borderWidth: 1.5,
    borderStyle: 'dashed',
    borderColor: colors.borderStrong,
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
  },
  placeholderLabel: { fontSize: typography.caption, color: colors.muted },
});
