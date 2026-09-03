'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import * as adminApi from './adminApi';
import { getToken, setToken } from './api';

interface AdminAuthContextValue {
  admin: adminApi.Admin | null;
  isReady: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AdminAuthContext = createContext<AdminAuthContextValue | null>(null);

const ADMIN_KEY = 'lajanm.admin.profile';

function readStoredAdmin(): adminApi.Admin | null {
  const token = getToken();
  const stored = window.localStorage.getItem(ADMIN_KEY);
  if (!token || !stored) return null;
  try {
    return JSON.parse(stored) as adminApi.Admin;
  } catch {
    setToken(null);
    return null;
  }
}

export function AdminAuthProvider({ children }: { children: React.ReactNode }) {
  const [admin, setAdmin] = useState<adminApi.Admin | null>(null);
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    // Restores the session from localStorage on page reload. This has to
    // run after mount (not as a useState lazy initializer) so the
    // client's first render matches the server-rendered markup —
    // window/localStorage don't exist during SSR, so reading them any
    // earlier would produce a hydration mismatch. The read itself is
    // deferred one microtask past mount (queueMicrotask) purely to keep
    // this effect's own execution free of a synchronous setState call.
    queueMicrotask(() => {
      setAdmin(readStoredAdmin());
      setIsReady(true);
    });
  }, []);

  const login = async (email: string, password: string) => {
    const result = await adminApi.login(email, password);
    setToken(result.accessToken);
    window.localStorage.setItem(ADMIN_KEY, JSON.stringify(result.admin));
    setAdmin(result.admin);
  };

  const logout = () => {
    setToken(null);
    window.localStorage.removeItem(ADMIN_KEY);
    setAdmin(null);
  };

  return (
    <AdminAuthContext.Provider value={{ admin, isReady, login, logout }}>
      {children}
    </AdminAuthContext.Provider>
  );
}

export function useAdminAuth(): AdminAuthContextValue {
  const ctx = useContext(AdminAuthContext);
  if (!ctx) throw new Error('useAdminAuth must be used within an AdminAuthProvider');
  return ctx;
}
