import type { ExpoConfig } from 'expo/config';

/**
 * Dynamic Expo config, replacing the previous static app.json.
 *
 * The reason it has to be dynamic: the API base URL is baked into the
 * binary at build time. A single hardcoded value means every build points
 * at the same server, so an APK built from a developer machine silently
 * ships `http://localhost:3000` and is dead on a real phone. EAS build
 * profiles (see eas.json) set LAJANM_API_URL per profile instead.
 */

// Reverse-DNS identifiers. These are permanent: once an app is published,
// changing them creates a *different* app in the stores and existing
// installs no longer update. Set them to a domain you control before the
// first store submission.
const IOS_BUNDLE_IDENTIFIER = process.env.LAJANM_IOS_BUNDLE_ID ?? 'com.lajanm.app';
const ANDROID_PACKAGE = process.env.LAJANM_ANDROID_PACKAGE ?? 'com.lajanm.app';

/**
 * No default: a build with no API URL configured must fail loudly at build
 * time rather than produce an installer that cannot reach anything. `expo
 * start` for local development sets it in package.json's dev script.
 */
const apiBaseUrl = process.env.LAJANM_API_URL;
if (!apiBaseUrl) {
  throw new Error(
    'LAJANM_API_URL is not set. Local dev: `npm run dev -w @lajanm/mobile`. ' +
      'Builds: set it in the eas.json profile.',
  );
}

// Android blocks cleartext HTTP by default, and a wallet must never send a
// bearer token over plain HTTP anyway. localhost is exempt so the emulator
// can still talk to a local API.
const isLocal = /^https?:\/\/(localhost|127\.0\.0\.1|10\.0\.2\.2)(:|$)/.test(apiBaseUrl);
if (!apiBaseUrl.startsWith('https://') && !isLocal) {
  throw new Error(`LAJANM_API_URL must use https:// (got: ${apiBaseUrl})`);
}

const config: ExpoConfig = {
  name: "Lajan'm",
  slug: 'lajanm',
  version: '1.0.0',
  orientation: 'portrait',
  icon: './assets/icon.png',
  userInterfaceStyle: 'light',
  ios: {
    supportsTablet: true,
    bundleIdentifier: IOS_BUNDLE_IDENTIFIER,
    // Incremented per store submission; EAS can manage this automatically
    // (see "autoIncrement" in eas.json's production profile).
    buildNumber: '1',
    infoPlist: {
      // Shown in the iOS Face ID prompt. Required: iOS rejects a build that
      // uses biometrics without this string.
      NSFaceIDUsageDescription:
        "Lajan'm itilize Face ID pou pwoteje aksè a kont ou.",
    },
  },
  android: {
    package: ANDROID_PACKAGE,
    // Must strictly increase on every Play Store upload.
    versionCode: 1,

    // KycCaptureScreen calls ImagePicker.launchCameraAsync(), which on
    // Android needs CAMERA declared in the manifest. Without it the ID/
    // selfie capture fails on a real device — a failure that never shows
    // up in Expo Go or a simulator, only in a built APK.
    permissions: ['android.permission.CAMERA'],

    // Everything a wallet does NOT need. Expo merges these in from
    // expo-image-picker and the base template, and each one is a real
    // cost: the store listing shows them, and reviewers scrutinise them.
    //
    // SYSTEM_ALERT_WINDOW is the serious one. "Draw over other apps" is
    // the standard Android overlay-attack vector used by banking trojans,
    // so a financial app requesting it invites both Play Store rejection
    // and an auditor's attention. It arrives in src/main (not just the
    // debug variant), so it would otherwise ship in release builds.
    blockedPermissions: [
      'android.permission.SYSTEM_ALERT_WINDOW',
      'android.permission.RECORD_AUDIO',
      'android.permission.READ_EXTERNAL_STORAGE',
      'android.permission.WRITE_EXTERNAL_STORAGE',
    ],
    adaptiveIcon: {
      backgroundColor: '#E6F4FE',
      foregroundImage: './assets/android-icon-foreground.png',
      backgroundImage: './assets/android-icon-background.png',
      monochromeImage: './assets/android-icon-monochrome.png',
    },
    predictiveBackGestureEnabled: false,
  },
  web: {
    favicon: './assets/favicon.png',
  },
  plugins: [
    'expo-secure-store',
    [
      'expo-image-picker',
      {
        cameraPermission:
          "Lajan'm bezwen aksè a kamera a pou verifye idantite ou (pyès idantite + selfi).",
        // The picker supports video, so it requests the microphone by
        // default. Lajan'm only ever takes stills for KYC — a wallet
        // asking for microphone access is an obvious trust problem.
        microphonePermission: false,
      },
    ],
  ],
  extra: {
    apiBaseUrl,
  },
};

export default config;
