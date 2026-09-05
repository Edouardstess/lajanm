import React, { useCallback, useEffect, useState } from 'react';
import { FlatList, StyleSheet, Text, View } from 'react-native';
import { DeviceSession, listDevices, revokeDevice } from '../api/auth';
import { Card } from '../components/Card';
import { EmptyState } from '../components/EmptyState';
import { Icon } from '../components/Icon';
import { PrimaryButton } from '../components/PrimaryButton';
import { formatDateTime } from '../format';
import { colors, fonts, radius, spacing, typography } from '../theme';
import { useTranslation } from '../i18n';

export function DevicesScreen() {
  const { t } = useTranslation();
  const [devices, setDevices] = useState<DeviceSession[]>([]);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setDevices(await listDevices());
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const onRevoke = async (id: string) => {
    await revokeDevice(id);
    load();
  };

  return (
    <View style={styles.container}>
      <FlatList
        data={devices}
        keyExtractor={(item) => item.id}
        refreshing={loading}
        onRefresh={load}
        contentContainerStyle={styles.content}
        ListEmptyComponent={loading ? null : <EmptyState icon="lock" title={t('profile.devices_empty')} />}
        renderItem={({ item }) => (
          <Card style={styles.card}>
            <View style={styles.header}>
              <View style={[styles.icon, item.revokedAt != null && styles.iconRevoked]}>
                <Icon name="lock" size={18} color={item.revokedAt ? colors.muted : colors.primary} />
              </View>
              <View style={styles.body}>
                <Text style={styles.name} numberOfLines={1}>
                  {item.deviceName ?? item.deviceId}
                </Text>
                <Text style={styles.lastSeen}>{formatDateTime(item.lastSeenAt)}</Text>
              </View>
            </View>
            {!item.revokedAt && (
              <PrimaryButton
                label={t('profile.revoke_device')}
                variant="secondary"
                onPress={() => onRevoke(item.id)}
              />
            )}
          </Card>
        )}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.ground },
  content: { padding: spacing.lg, flexGrow: 1 },
  card: { marginBottom: spacing.sm + 2 },
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.md - 3 },
  icon: {
    width: 40,
    height: 40,
    borderRadius: radius.sm,
    backgroundColor: colors.primarySoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  iconRevoked: { backgroundColor: colors.surfaceAlt },
  body: { flex: 1 },
  name: { fontSize: typography.label, fontFamily: fonts.semibold, color: colors.text },
  lastSeen: { fontSize: typography.overline, fontFamily: fonts.regular, color: colors.muted, marginTop: 3 },
});
