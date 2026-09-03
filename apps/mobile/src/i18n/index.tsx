import AsyncStorage from '@react-native-async-storage/async-storage';
import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import en from './locales/en';
import fr from './locales/fr';
import ht from './locales/ht';

export type Locale = 'ht' | 'fr' | 'en';
export const SUPPORTED_LOCALES: Locale[] = ['ht', 'fr', 'en'];
// Kreyòl is the default per the product requirements — most users are more
// comfortable in kreyòl than in French, and it must never silently fall
// back to French.
const DEFAULT_LOCALE: Locale = 'ht';
const STORAGE_KEY = 'lajanm.locale';

const catalogs: Record<Locale, typeof ht> = { ht, fr, en };

type Catalog = typeof ht;

function resolve(catalog: Catalog, key: string): string | undefined {
  return key.split('.').reduce<unknown>((node, segment) => {
    if (node && typeof node === 'object' && segment in node) {
      return (node as Record<string, unknown>)[segment];
    }
    return undefined;
  }, catalog) as string | undefined;
}

interface I18nContextValue {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  t: (key: string) => string;
}

const I18nContext = createContext<I18nContextValue | null>(null);

export function I18nProvider({ children }: { children: React.ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(DEFAULT_LOCALE);

  useEffect(() => {
    AsyncStorage.getItem(STORAGE_KEY).then((stored) => {
      if (stored && SUPPORTED_LOCALES.includes(stored as Locale)) {
        setLocaleState(stored as Locale);
      }
    });
  }, []);

  const setLocale = (next: Locale) => {
    setLocaleState(next);
    AsyncStorage.setItem(STORAGE_KEY, next).catch(() => {
      // Non-fatal: the in-memory locale for this session is still correct,
      // only the persisted preference for next launch failed to save.
    });
  };

  const t = (key: string): string => {
    const value = resolve(catalogs[locale], key) ?? resolve(catalogs[DEFAULT_LOCALE], key);
    return value ?? key;
  };

  const value = useMemo(() => ({ locale, setLocale, t }), [locale]);

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useTranslation(): I18nContextValue {
  const ctx = useContext(I18nContext);
  if (!ctx) throw new Error('useTranslation must be used within an I18nProvider');
  return ctx;
}
