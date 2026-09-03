import React, { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { ApiError } from '../api/client';
import { createTicket, TicketCategory } from '../api/support';
import { PrimaryButton } from '../components/PrimaryButton';
import { useTranslation } from '../i18n';
import { colors, spacing, touchTarget, typography } from '../theme';

const CATEGORIES: TicketCategory[] = ['general', 'transaction', 'kyc', 'technical', 'other'];

export function NewTicketScreen({ navigation }: { navigation: { goBack: () => void } }) {
  const { t } = useTranslation();
  const [subject, setSubject] = useState('');
  const [category, setCategory] = useState<TicketCategory>('general');
  const [message, setMessage] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async () => {
    if (!subject.trim() || !message.trim()) return;

    setSubmitting(true);
    setError(null);
    try {
      await createTicket(subject.trim(), message.trim(), category);
      navigation.goBack();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('common.error_generic'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.label}>{t('support.subject_label')}</Text>
      <TextInput style={styles.input} value={subject} onChangeText={setSubject} maxLength={255} />

      <Text style={styles.label}>{t('support.category_label')}</Text>
      <View style={styles.chips}>
        {CATEGORIES.map((option) => (
          <Pressable
            key={option}
            accessibilityRole="button"
            accessibilityState={{ selected: category === option }}
            style={[styles.chip, category === option && styles.chipSelected]}
            onPress={() => setCategory(option)}
          >
            <Text style={[styles.chipLabel, category === option && styles.chipLabelSelected]}>
              {t(`support.category_${option}`)}
            </Text>
          </Pressable>
        ))}
      </View>

      <Text style={styles.label}>{t('support.message_label')}</Text>
      <TextInput
        style={[styles.input, styles.textArea]}
        value={message}
        onChangeText={setMessage}
        multiline
        numberOfLines={6}
        textAlignVertical="top"
      />

      {error && <Text style={styles.errorText}>{error}</Text>}

      <PrimaryButton
        label={t('support.send_button')}
        onPress={onSubmit}
        loading={submitting}
        disabled={!subject.trim() || !message.trim()}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background },
  content: { padding: spacing.lg },
  label: { fontSize: typography.label, color: colors.text, marginBottom: spacing.xs },
  input: {
    minHeight: touchTarget.minHeight,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    paddingHorizontal: spacing.md,
    fontSize: typography.body,
    color: colors.text,
    marginBottom: spacing.md,
  },
  textArea: { minHeight: 140, paddingTop: spacing.md },
  chips: { flexDirection: 'row', flexWrap: 'wrap', marginBottom: spacing.md },
  chip: {
    minHeight: touchTarget.minHeight,
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    paddingHorizontal: spacing.md,
    marginRight: spacing.sm,
    marginBottom: spacing.sm,
  },
  chipSelected: { backgroundColor: colors.primary, borderColor: colors.primary },
  chipLabel: { fontSize: typography.label, color: colors.text },
  chipLabelSelected: { color: colors.primaryText, fontWeight: '600' },
  errorText: { color: colors.danger, marginBottom: spacing.md, textAlign: 'center' },
});
