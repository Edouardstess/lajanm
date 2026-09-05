import React, { useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { ApiError } from '../api/client';
import { createTicket, TicketCategory } from '../api/support';
import { Chip } from '../components/Chip';
import { Field } from '../components/Field';
import { InfoNote } from '../components/InfoNote';
import { PrimaryButton } from '../components/PrimaryButton';
import { useTranslation } from '../i18n';
import { colors, fonts, spacing, typography } from '../theme';

const CATEGORIES: TicketCategory[] = ['general', 'transaction', 'kyc', 'technical', 'other'];

export function NewTicketScreen({ navigation }: { navigation: { goBack: () => void } }) {
  const { t } = useTranslation();
  const [subject, setSubject] = useState('');
  const [category, setCategory] = useState<TicketCategory>('general');
  const [message, setMessage] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const ready = subject.trim().length > 0 && message.trim().length > 0;

  const onSubmit = async () => {
    if (!ready) return;

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
      <Field label={t('support.subject_label')} value={subject} onChangeText={setSubject} maxLength={255} />

      <View style={styles.categoryBlock}>
        <Text style={styles.categoryLabel}>{t('support.category_label')}</Text>
        <View style={styles.categories}>
          {CATEGORIES.map((option) => (
            <Chip
              key={option}
              label={t(`support.category_${option}`)}
              selected={category === option}
              onPress={() => setCategory(option)}
            />
          ))}
        </View>
      </View>

      <Field
        label={t('support.message_label')}
        value={message}
        onChangeText={setMessage}
        multiline
        numberOfLines={6}
        textAlignVertical="top"
        style={styles.textArea}
      />

      {error && <InfoNote tone="danger">{error}</InfoNote>}

      <PrimaryButton
        icon="send"
        label={t('support.send_button')}
        onPress={onSubmit}
        loading={submitting}
        disabled={!ready}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.ground },
  content: { padding: spacing.lg, paddingBottom: spacing.xl },
  categoryBlock: { marginBottom: spacing.md },
  categoryLabel: {
    fontSize: typography.label,
    fontFamily: fonts.semibold,
    color: colors.text,
    marginBottom: spacing.sm,
  },
  categories: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm },
  textArea: { minHeight: 140, paddingTop: spacing.md, lineHeight: typography.body + 7 },
});
