import AsyncStorage from '@react-native-async-storage/async-storage';
import * as Crypto from 'expo-crypto';
import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import * as authApi from '../api/auth';
import { setToken } from '../api/client';

const DEVICE_ID_KEY = 'lajanm.deviceId';

async function getOrCreateDeviceId(): Promise<string> {
  const existing = await AsyncStorage.getItem(DEVICE_ID_KEY);
  if (existing) return existing;
  const generated = Crypto.randomUUID();
  await AsyncStorage.setItem(DEVICE_ID_KEY, generated);
  return generated;
}

interface AuthContextValue {
  user: authApi.AuthUser | null;
  isReady: boolean;
  login: (phone: string, pin: string) => Promise<void>;
  register: (phone: string, pin: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshProfile: (fields: { fullName?: string; email?: string }) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<authApi.AuthUser | null>(null);
  // Whether the initial session restore attempt has finished — used to
  // avoid flashing the login screen before we've checked for a stored
  // session on cold start.
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    // This MVP does not persist the session across cold starts (no
    // refresh-token flow yet) — a stored access token could be expired
    // with no way to silently renew it. Session restore lands with the
    // OTP/session-lifecycle work in the security module.
    setIsReady(true);
  }, []);

  const login = async (phone: string, pin: string) => {
    const deviceId = await getOrCreateDeviceId();
    const result = await authApi.login(phone, pin, deviceId);
    await setToken(result.accessToken);
    setUser(result.user);
  };

  const register = async (phone: string, pin: string) => {
    await authApi.register(phone, pin);
    await login(phone, pin);
  };

  const logout = async () => {
    await setToken(null);
    setUser(null);
  };

  const refreshProfile = async (fields: { fullName?: string; email?: string }) => {
    const updated = await authApi.updateProfile(fields);
    setUser(updated);
  };

  const value = useMemo(
    () => ({ user, isReady, login, register, logout, refreshProfile }),
    [user, isReady],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
