import AsyncStorage from '@react-native-async-storage/async-storage';
import { useCallback, useEffect, useRef, useState } from 'react';
import { BalanceSnapshot, getBalance } from '../api/wallet';

const CACHE_KEY = 'lajanm.balanceCache';

/**
 * Shows the last known balance immediately (from local cache) so the
 * screen isn't blank while the network request is in flight or unreachable
 * — but every render that uses cached data must show `isFromCache` and
 * `asOf` so the UI can be honest about freshness (NF-04 / offline
 * behavior requirement: cached data is fine to display, silently treating
 * it as live is not).
 */
export function useBalance() {
  const [snapshot, setSnapshot] = useState<BalanceSnapshot | null>(null);
  const [isFromCache, setIsFromCache] = useState(false);
  const [loading, setLoading] = useState(true);
  const hasDataRef = useRef(false);

  const loadFromCache = useCallback(async () => {
    try {
      const raw = await AsyncStorage.getItem(CACHE_KEY);
      if (raw) {
        setSnapshot(JSON.parse(raw));
        setIsFromCache(true);
        hasDataRef.current = true;
      }
    } catch {
      // No cache available — the live fetch below is still attempted.
    }
  }, []);

  const refresh = useCallback(async () => {
    try {
      const fresh = await getBalance();
      setSnapshot(fresh);
      setIsFromCache(false);
      hasDataRef.current = true;
      AsyncStorage.setItem(CACHE_KEY, JSON.stringify(fresh)).catch(() => {});
    } catch {
      // Network/API failure: keep showing whatever we already have
      // (cached or previous live value) rather than an error screen for
      // something as routine as a dropped connection.
      if (hasDataRef.current) setIsFromCache(true);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadFromCache().finally(refresh);
  }, [loadFromCache, refresh]);

  return { snapshot, isFromCache, loading, refresh };
}
