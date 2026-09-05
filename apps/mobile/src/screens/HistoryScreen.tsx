import React, { useCallback, useEffect, useState } from 'react';
import { FlatList, StyleSheet, View } from 'react-native';
import { getHistory, WalletHistoryEntry } from '../api/wallet';
import { EmptyState } from '../components/EmptyState';
import { formatDateTime } from '../format';
import { TransactionRow } from '../components/TransactionRow';
import { useTranslation } from '../i18n';
import { colors, spacing } from '../theme';

export function HistoryScreen() {
  const { t } = useTranslation();
  const [entries, setEntries] = useState<WalletHistoryEntry[]>([]);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setEntries(await getHistory({ limit: 50 }));
    } catch {
      // Keep whatever was previously loaded; a pull-to-refresh retry is
      // available via onRefresh below.
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <View style={styles.container}>
      <FlatList
        data={entries}
        keyExtractor={(item) => item.id}
        refreshing={loading}
        onRefresh={load}
        contentContainerStyle={styles.content}
        ListEmptyComponent={loading ? null : <EmptyState title={t('wallet.history_empty')} />}
        renderItem={({ item }) => (
          <TransactionRow
            title={t(`wallet.type_${item.operationType}`)}
            date={formatDateTime(item.createdAt)}
            amountMinor={item.amountMinor}
            currency={item.currency}
            direction={item.direction}
          />
        )}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.ground },
  content: { padding: spacing.lg, flexGrow: 1 },
});
