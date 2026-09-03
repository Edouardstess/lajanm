import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useAuth } from '../context/AuthContext';
import { colors, spacing, typography } from '../theme';

/**
 * Placeholder landing screen. Balance/transfer/history land here in the
 * wallet module (Module 3); top-up/payout in Modules 2 and 4.
 */
export function HomeScreen() {
  const { user } = useAuth();
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Lajan&apos;m</Text>
      <Text style={styles.subtitle}>{user?.phone}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, alignItems: 'center', justifyContent: 'center' },
  title: { fontSize: typography.title, fontWeight: '700', color: colors.text },
  subtitle: { fontSize: typography.body, color: colors.muted, marginTop: spacing.sm },
});
