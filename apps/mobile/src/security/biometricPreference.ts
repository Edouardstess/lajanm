import * as SecureStore from 'expo-secure-store';

const KEY = 'lajanm.biometricLockEnabled';

/**
 * Per-device preference only — never sent to or read from the server.
 * The backend has no concept of "biometric enabled"; it only ever sees a
 * PIN/JWT (see docs/architecture.md's note on why no biometric data, not
 * even a flag, is stored server-side).
 */
export async function isBiometricLockEnabled(): Promise<boolean> {
  return (await SecureStore.getItemAsync(KEY)) === 'true';
}

export async function setBiometricLockEnabled(enabled: boolean): Promise<void> {
  await SecureStore.setItemAsync(KEY, enabled ? 'true' : 'false');
}
