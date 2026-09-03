import React, { useCallback, useEffect, useState } from 'react';
import { FlatList, StyleSheet, Text, View } from 'react-native';
import { getHistory, WalletHistoryEntry } from '../api/wallet';
import { useTranslation } from '../i18n';
import { colors, spacing, typography } from '../theme';

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

  const typeLabel = (type: WalletHistoryEntry['operationType']) => t(`wallet.type_${type}`);

  return (
    <View style={styles.container}>
      <FlatList
        data={entries}
        keyExtractor={(item) => item.id}
        refreshing={loading}
        onRefresh={load}
        ListEmptyComponent={<Text style={styles.empty}>{t('wallet.history_empty')}</Text>}
        renderItem={({ item }) => (
          <View style={styles.row}>
            <View>
              <Text style={styles.type}>{typeLabel(item.operationType)}</Text>
              <Text style={styles.date}>{new Date(item.createdAt).toLocaleString()}</Text>
            </View>
            <Text style={[styles.amount, item.direction === 'credit' ? styles.credit : styles.debit]}>
              {item.direction === 'credit' ? '+' : '-'}
              {(Number(item.amountMinor) / 100).toFixed(2)} {item.currency}
            </Text>
          </View>
        )}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, padding: spacing.lg },
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  type: { fontSize: typography.body, color: colors.text, fontWeight: '600' },
  date: { fontSize: typography.label, color: colors.muted },
  amount: { fontSize: typography.body, fontWeight: '700' },
  credit: { color: colors.success },
  debit: { color: colors.text },
  empty: { textAlign: 'center', color: colors.muted, marginTop: spacing.xl },
});
