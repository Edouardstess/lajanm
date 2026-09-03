import React, { useCallback, useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { FaqEntry, listFaq, listMyTickets, SupportTicket } from '../api/support';
import { PrimaryButton } from '../components/PrimaryButton';
import { useTranslation } from '../i18n';
import { colors, spacing, touchTarget, typography } from '../theme';

/**
 * The help hub: FAQ first (most questions are answered without ever
 * opening a ticket, which matters when support is a small team), then the
 * customer's own requests.
 */
export function SupportScreen({ navigation }: { navigation: { navigate: (screen: string, params?: object) => void } }) {
  const { t } = useTranslation();
  const [faqs, setFaqs] = useState<FaqEntry[]>([]);
  const [tickets, setTickets] = useState<SupportTicket[]>([]);
  const [expandedFaq, setExpandedFaq] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [faqList, ticketList] = await Promise.all([listFaq(), listMyTickets()]);
      setFaqs(faqList);
      setTickets(ticketList);
    } catch {
      // Offline or server error: keep whatever was already on screen rather
      // than blanking the help section, which is exactly where a user goes
      // when something else is already broken.
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <PrimaryButton label={t('support.new_ticket')} onPress={() => navigation.navigate('SupportNewTicket')} />

      <Text style={styles.sectionTitle}>{t('support.tickets_title')}</Text>
      {tickets.length === 0 && !loading && <Text style={styles.empty}>{t('support.tickets_empty')}</Text>}
      {tickets.map((ticket) => (
        <Pressable
          key={ticket.id}
          style={styles.row}
          accessibilityRole="button"
          onPress={() => navigation.navigate('SupportTicket', { ticketId: ticket.id })}
        >
          <Text style={styles.rowTitle}>{ticket.subject}</Text>
          <Text style={styles.rowMeta}>
            {t(`support.status_${ticket.status}`)} · {new Date(ticket.updatedAt).toLocaleDateString()}
          </Text>
        </Pressable>
      ))}

      <Text style={styles.sectionTitle}>{t('support.faq_title')}</Text>
      {faqs.length === 0 && !loading && <Text style={styles.empty}>{t('support.faq_empty')}</Text>}
      {faqs.map((faq) => (
        <Pressable
          key={faq.id}
          style={styles.row}
          accessibilityRole="button"
          onPress={() => setExpandedFaq(expandedFaq === faq.id ? null : faq.id)}
        >
          <Text style={styles.rowTitle}>{faq.question}</Text>
          {expandedFaq === faq.id && <Text style={styles.answer}>{faq.answer}</Text>}
        </Pressable>
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background },
  content: { padding: spacing.lg },
  sectionTitle: {
    fontSize: typography.title,
    color: colors.text,
    fontWeight: '700',
    marginTop: spacing.lg,
    marginBottom: spacing.sm,
  },
  row: {
    justifyContent: 'center',
    minHeight: touchTarget.minHeight,
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  rowTitle: { fontSize: typography.body, color: colors.text, fontWeight: '600' },
  rowMeta: { fontSize: typography.label, color: colors.muted, marginTop: spacing.xs },
  answer: { fontSize: typography.body, color: colors.text, marginTop: spacing.sm, lineHeight: 24 },
  empty: { fontSize: typography.body, color: colors.muted, marginTop: spacing.sm },
});
