import React, { useCallback, useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { FaqEntry, listFaq, listMyTickets, SupportTicket } from '../api/support';
import { Card } from '../components/Card';
import { EmptyState } from '../components/EmptyState';
import { Icon } from '../components/Icon';
import { PrimaryButton } from '../components/PrimaryButton';
import { SectionHeader } from '../components/SectionHeader';
import { formatDate } from '../format';
import { useTranslation } from '../i18n';
import { colors, fonts, radius, spacing, typography } from '../theme';

const OPEN_STATUSES: SupportTicket['status'][] = ['open', 'in_progress'];

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
      {/* La FAQ passe avant le bouton « nouvelle demande » : la plupart
          des questions y trouvent leur réponse, et un ticket de moins,
          c'est une réponse plus rapide pour ceux qui en ouvrent un. */}
      <SectionHeader title={t('support.faq_title')} />
      {faqs.length === 0 && !loading && <EmptyState icon="help" title={t('support.faq_empty')} />}
      {faqs.map((faq) => {
        const expanded = expandedFaq === faq.id;
        return (
          <Pressable
            key={faq.id}
            accessibilityRole="button"
            accessibilityState={{ expanded }}
            onPress={() => setExpandedFaq(expanded ? null : faq.id)}
            style={({ pressed }) => [styles.faq, pressed && styles.pressed]}
          >
            <View style={styles.faqHeader}>
              <Text style={styles.faqQuestion}>{faq.question}</Text>
              <Icon name={expanded ? 'arrow-up' : 'arrow-down'} size={17} color={colors.muted} />
            </View>
            {expanded && <Text style={styles.faqAnswer}>{faq.answer}</Text>}
          </Pressable>
        );
      })}

      <SectionHeader title={t('support.tickets_title')} />
      {tickets.length === 0 && !loading ? (
        <EmptyState title={t('support.tickets_empty')} />
      ) : (
        tickets.map((ticket) => {
          const open = OPEN_STATUSES.includes(ticket.status);
          return (
            <Pressable
              key={ticket.id}
              accessibilityRole="button"
              onPress={() => navigation.navigate('SupportTicket', { ticketId: ticket.id })}
              style={({ pressed }) => [pressed && styles.pressed]}
            >
              <Card flat style={styles.ticket}>
                <View style={styles.ticketBody}>
                  <Text style={styles.ticketSubject} numberOfLines={1}>
                    {ticket.subject}
                  </Text>
                  <Text style={styles.ticketDate}>{formatDate(ticket.updatedAt)}</Text>
                </View>
                <View style={[styles.status, open ? styles.statusOpen : styles.statusClosed]}>
                  <Text style={[styles.statusLabel, open && styles.statusLabelOpen]}>
                    {t(`support.status_${ticket.status}`)}
                  </Text>
                </View>
              </Card>
            </Pressable>
          );
        })
      )}

      <View style={styles.cta}>
        <PrimaryButton
          icon="plus"
          label={t('support.new_ticket')}
          onPress={() => navigation.navigate('SupportNewTicket')}
        />
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.ground },
  content: { padding: spacing.lg, paddingBottom: spacing.xl },
  pressed: { opacity: 0.75 },
  faq: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    padding: spacing.md - 2,
    marginBottom: spacing.sm,
  },
  faqHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm },
  faqQuestion: { flex: 1, fontSize: typography.label, fontFamily: fonts.semibold, color: colors.text, lineHeight: 21 },
  faqAnswer: {
    fontSize: typography.label - 1, fontFamily: fonts.regular,
    color: colors.muted,
    lineHeight: 22,
    marginTop: spacing.sm + 2,
  },
  ticket: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, marginBottom: spacing.sm },
  ticketBody: { flex: 1 },
  ticketSubject: { fontSize: typography.label, fontFamily: fonts.semibold, color: colors.text },
  ticketDate: { fontSize: typography.overline, fontFamily: fonts.regular, color: colors.muted, marginTop: 3 },
  status: { borderRadius: radius.pill, paddingHorizontal: spacing.sm + 2, paddingVertical: 5 },
  statusOpen: { backgroundColor: colors.warningSoft },
  statusClosed: { backgroundColor: colors.surfaceAlt },
  statusLabel: { fontSize: 11, fontFamily: fonts.semibold, color: colors.muted },
  statusLabelOpen: { color: colors.onAccent },
  cta: { marginTop: spacing.lg },
});
