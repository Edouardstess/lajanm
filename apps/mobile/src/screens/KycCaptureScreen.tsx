import * as ImagePicker from 'expo-image-picker';
import React, { useState } from 'react';
import { Image, ScrollView, StyleSheet, Text, View } from 'react-native';
import { ApiError } from '../api/client';
import { submitKyc } from '../api/kyc';
import { PrimaryButton } from '../components/PrimaryButton';
import { useTranslation } from '../i18n';
import { colors, spacing, typography } from '../theme';

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

  if (status === 'submitted') {
    return (
      <View style={styles.container}>
        <Text style={styles.title}>{t('kyc.status_pending')}</Text>
      </View>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.title}>{t('kyc.title')}</Text>
      <Text style={styles.explanation}>{t('kyc.explanation')}</Text>

      <View style={styles.captureRow}>
        {idDocumentUri && <Image source={{ uri: idDocumentUri }} style={styles.preview} />}
        <PrimaryButton
          label={t('kyc.id_document_button')}
          variant={idDocumentUri ? 'secondary' : 'primary'}
          onPress={() => capture(setIdDocumentUri)}
        />
      </View>

      <View style={styles.captureRow}>
        {selfieUri && <Image source={{ uri: selfieUri }} style={styles.preview} />}
        <PrimaryButton
          label={t('kyc.selfie_button')}
          variant={selfieUri ? 'secondary' : 'primary'}
          onPress={() => capture(setSelfieUri)}
        />
      </View>

      {error && <Text style={styles.error}>{error}</Text>}

      <PrimaryButton
        label={t('kyc.submit_button')}
        onPress={onSubmit}
        loading={submitting}
        disabled={!idDocumentUri || !selfieUri}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, backgroundColor: colors.background, padding: spacing.lg },
  title: { fontSize: typography.title, fontWeight: '700', color: colors.text, marginBottom: spacing.sm },
  explanation: { fontSize: typography.body, color: colors.muted, marginBottom: spacing.lg },
  captureRow: { marginBottom: spacing.lg },
  preview: { width: '100%', height: 180, borderRadius: 12, marginBottom: spacing.sm, backgroundColor: colors.border },
  error: { color: colors.danger, marginBottom: spacing.md },
});
