import React, { useCallback, useEffect, useState } from 'react';
import { FlatList, StyleSheet, Text, View } from 'react-native';
import { DeviceSession, listDevices, revokeDevice } from '../api/auth';
import { PrimaryButton } from '../components/PrimaryButton';
import { useTranslation } from '../i18n';
import { colors, spacing, typography } from '../theme';

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
      <Text style={styles.title}>{t('profile.devices_title')}</Text>
      <FlatList
        data={devices}
        keyExtractor={(item) => item.id}
        refreshing={loading}
        onRefresh={load}
        renderItem={({ item }) => (
          <View style={styles.row}>
            <Text style={styles.deviceName}>{item.deviceName ?? item.deviceId}</Text>
            <Text style={styles.lastSeen}>{new Date(item.lastSeenAt).toLocaleString()}</Text>
            {!item.revokedAt && (
              <PrimaryButton
                label={t('profile.revoke_device')}
                variant="secondary"
                onPress={() => onRevoke(item.id)}
              />
            )}
          </View>
        )}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, padding: spacing.lg },
  title: { fontSize: typography.title, fontWeight: '700', color: colors.text, marginBottom: spacing.md },
  row: { paddingVertical: spacing.md, borderBottomWidth: 1, borderBottomColor: colors.border },
  deviceName: { fontSize: typography.body, color: colors.text, fontWeight: '600' },
  lastSeen: { fontSize: typography.label, color: colors.muted, marginBottom: spacing.xs },
});
