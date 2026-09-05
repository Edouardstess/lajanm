import * as LocalAuthentication from 'expo-local-authentication';
import React, { useEffect, useRef, useState } from 'react';
import { AppState, AppStateStatus, StyleSheet, Text, View } from 'react-native';
import { Icon } from '../components/Icon';
import { PrimaryButton } from '../components/PrimaryButton';
import { useTranslation } from '../i18n';
import { colors, radius, spacing, typography } from '../theme';
import { isBiometricLockEnabled } from './biometricPreference';

const BACKGROUND_LOCK_DELAY_MS = 30_000;

/**
 * Wraps the authenticated app so returning from the background (or first
 * opening the app this session) requires biometric/device-passcode
 * re-authentication when the user has opted in — the client-side half of
 * NF-05/NF-08's "verrouillage automatique après une période d'inactivité".
 * A short grace period (BACKGROUND_LOCK_DELAY_MS) avoids re-prompting for
 * a quick app-switch, e.g. copying an OTP from the SMS app.
 */
export function AppLockGate({ children }: { children: React.ReactNode }) {
  const { t } = useTranslation();
  const [locked, setLocked] = useState(false);
  const [checked, setChecked] = useState(false);
  const backgroundedAt = useRef<number | null>(null);

  const tryUnlock = async () => {
    const enabled = await isBiometricLockEnabled();
    if (!enabled) {
      setLocked(false);
      setChecked(true);
      return;
    }

    const hasHardware = await LocalAuthentication.hasHardwareAsync();
    const isEnrolled = await LocalAuthentication.isEnrolledAsync();
    if (!hasHardware || !isEnrolled) {
      // Nothing to authenticate against on this device — don't lock the
      // user out of their own app because of a setting that can't apply.
      setLocked(false);
      setChecked(true);
      return;
    }

    const result = await LocalAuthentication.authenticateAsync();
    setLocked(!result.success);
    setChecked(true);
  };

  useEffect(() => {
    tryUnlock();

    const subscription = AppState.addEventListener('change', (next: AppStateStatus) => {
      if (next === 'background' || next === 'inactive') {
        backgroundedAt.current = Date.now();
      } else if (next === 'active' && backgroundedAt.current) {
        const elapsed = Date.now() - backgroundedAt.current;
        backgroundedAt.current = null;
        if (elapsed >= BACKGROUND_LOCK_DELAY_MS) {
          tryUnlock();
        }
      }
    });

    return () => subscription.remove();
  }, []);

  if (!checked) return null;

  if (locked) {
    return (
      <View style={styles.container}>
        <View style={styles.badge}>
          <Icon name="lock" size={34} color={colors.primary} />
        </View>
        <Text style={styles.title}>{t('security.locked_title')}</Text>
        <View style={styles.action}>
          <PrimaryButton label={t('security.unlock_button')} onPress={tryUnlock} />
        </View>
      </View>
    );
  }

  return <>{children}</>;
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.ground,
    alignItems: 'center',
    justifyContent: 'center',
    padding: spacing.lg,
  },
  badge: {
    width: 84,
    height: 84,
    borderRadius: radius.xl,
    backgroundColor: colors.primarySoft,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.lg,
  },
  title: {
    fontSize: typography.title,
    fontWeight: '700',
    color: colors.text,
    textAlign: 'center',
    letterSpacing: -0.3,
  },
  action: { alignSelf: 'stretch', marginTop: spacing.lg },
});
