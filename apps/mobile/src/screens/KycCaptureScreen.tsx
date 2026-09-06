import * as ImagePicker from 'expo-image-picker';
import React, { useState } from 'react';
import { ActivityIndicator, Image, StyleSheet, Text, View } from 'react-native';
import { ApiError, UnsupportedFileError, uploadKycPhoto } from '../api/client';
import { submitKyc } from '../api/kyc';
import { Icon } from '../components/Icon';
import { InfoNote } from '../components/InfoNote';
import { PrimaryButton } from '../components/PrimaryButton';
import { Screen } from '../components/Screen';
import { StatusView } from '../components/StatusView';
import { useTranslation } from '../i18n';
import { colors, fonts, radius, spacing, typography } from '../theme';

/**
 * Captures and submits the two KYC photos. There is no object-storage
 * upload step yet (see SubmitKycDto) — the captured local file URIs are
 * sent as-is. This is a known placeholder, not a finished upload pipeline;
 * wiring real object storage is a follow-up infra task.
 */
export function KycCaptureScreen() {
  const { t } = useTranslation();
  // Chaque photo existe sous deux formes : l'URI locale, pour l'afficher,
  // et l'identifiant renvoyé par le serveur une fois le fichier inspecté
  // et accepté. Seul le second permet d'envoyer la demande.
  const [idDocument, setIdDocument] = useState<Capture>(EMPTY_CAPTURE);
  const [selfie, setSelfie] = useState<Capture>(EMPTY_CAPTURE);
  const [submitting, setSubmitting] = useState(false);
  const [status, setStatus] = useState<'idle' | 'submitted' | 'error'>('idle');
  const [error, setError] = useState<string | null>(null);

  /**
   * Prend la photo, puis la dépose immédiatement.
   *
   * L'envoi se fait à la prise et non à la validation finale : sur un
   * réseau EDGE, faire monter deux photos d'un coup après le dernier
   * appui donne une attente que l'utilisateur interprète comme un
   * blocage. Ici chaque photo est confirmée dès qu'elle est acceptée.
   */
  const capture = async (
    current: Capture,
    onChange: (next: Capture) => void,
  ) => {
    const permission = await ImagePicker.requestCameraPermissionsAsync();
    if (!permission.granted) return;

    const result = await ImagePicker.launchCameraAsync({ quality: 0.6 });
    if (result.canceled || !result.assets[0]) return;

    const uri = result.assets[0].uri;
    setError(null);
    onChange({ uri, fileId: null, uploading: true });

    try {
      const { fileId } = await uploadKycPhoto(uri);
      onChange({ uri, fileId, uploading: false });
    } catch (err) {
      onChange({ uri: null, fileId: null, uploading: false });
      if (err instanceof UnsupportedFileError) {
        setError(t('kyc.unsupported_file'));
      } else if (err instanceof ApiError) {
        // Le serveur explique précisément ce qu'il a refusé (contenu qui
        // n'est pas une image, fichier trop lourd...) : le message brut
        // est plus utile qu'un générique.
        setError(err.message);
      } else {
        setError(t('kyc.upload_failed'));
      }
      void current;
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
    if (!idDocument.fileId || !selfie.fileId) return;
    setSubmitting(true);
    setError(null);
    try {
      await submitKyc(idDocument.fileId, selfie.fileId);
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
          disabled={!idDocument.fileId || !selfie.fileId}
        />
      }
    >
      <CaptureSlot
        label={t('kyc.id_document_button')}
        capture={idDocument}
        uploadingLabel={t('kyc.uploading')}
        readyLabel={t('kyc.photo_accepted')}
        onPress={() => capture(idDocument, setIdDocument)}
      />
      <CaptureSlot
        label={t('kyc.selfie_button')}
        capture={selfie}
        uploadingLabel={t('kyc.uploading')}
        readyLabel={t('kyc.photo_accepted')}
        onPress={() => capture(selfie, setSelfie)}
      />

      {error && <InfoNote tone="danger">{error}</InfoNote>}
    </Screen>
  );
}

interface Capture {
  uri: string | null;
  /** Renseigné seulement une fois le fichier accepté par le serveur. */
  fileId: string | null;
  uploading: boolean;
}

const EMPTY_CAPTURE: Capture = { uri: null, fileId: null, uploading: false };

/**
 * Un emplacement par photo, qui montre la photo prise plutôt que de se
 * contenter de changer la couleur d'un bouton : l'utilisateur doit voir
 * ce qu'il envoie, une pièce d'identité floue étant la première cause de
 * refus de vérification.
 *
 * Le bandeau distingue « photo prise » de « photo acceptée » : tant que
 * le serveur ne l'a pas validée, la demande ne peut pas partir, et il
 * faut que ça se voie.
 */
function CaptureSlot({
  label,
  capture,
  uploadingLabel,
  readyLabel,
  onPress,
}: {
  label: string;
  capture: Capture;
  uploadingLabel: string;
  readyLabel: string;
  onPress: () => void;
}) {
  return (
    <View style={styles.slot}>
      {capture.uri ? (
        <View>
          <Image source={{ uri: capture.uri }} style={styles.preview} accessibilityIgnoresInvertColors />
          <View style={[styles.stamp, capture.fileId ? styles.stampReady : styles.stampPending]}>
            {capture.uploading ? (
              <ActivityIndicator size="small" color={colors.primaryText} />
            ) : (
              <Icon name="check" size={14} color={colors.primaryText} />
            )}
            <Text style={styles.stampLabel}>{capture.uploading ? uploadingLabel : readyLabel}</Text>
          </View>
        </View>
      ) : (
        <View style={styles.placeholder}>
          <Icon name="card" size={28} color={colors.placeholder} />
          <Text style={styles.placeholderLabel}>{label}</Text>
        </View>
      )}
      <PrimaryButton
        label={label}
        variant={capture.fileId ? 'quiet' : 'secondary'}
        onPress={onPress}
        disabled={capture.uploading}
      />
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
  placeholderLabel: { fontSize: typography.caption, fontFamily: fonts.regular, color: colors.muted },
  stamp: {
    position: 'absolute',
    left: spacing.sm,
    bottom: spacing.sm,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs + 2,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.sm + 2,
    paddingVertical: 6,
  },
  stampPending: { backgroundColor: colors.accentInk },
  stampReady: { backgroundColor: colors.success },
  stampLabel: { fontSize: 11, fontFamily: fonts.semibold, color: colors.primaryText },
});
