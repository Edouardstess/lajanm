import { useRoute } from '@react-navigation/native';
import React, { useCallback, useEffect, useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { ApiError } from '../api/client';
import { addMessage, getTicket, SupportMessage, SupportTicket } from '../api/support';
import { Field } from '../components/Field';
import { InfoNote } from '../components/InfoNote';
import { PrimaryButton } from '../components/PrimaryButton';
import { formatDateTime } from '../format';
import { useTranslation } from '../i18n';
import { colors, radius, spacing, typography } from '../theme';

export function TicketThreadScreen() {
  // The stack navigator is untyped (see RootNavigator), so params arrive as
  // `object | undefined`. Reading them through useRoute keeps the cast in
  // one place here instead of forcing a prop type the navigator can't
  // satisfy.
  const { ticketId } = useRoute().params as { ticketId: string };
  const { t } = useTranslation();
  const [ticket, setTicket] = useState<SupportTicket | null>(null);
  const [messages, setMessages] = useState<SupportMessage[]>([]);
  const [reply, setReply] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [sending, setSending] = useState(false);

  const load = useCallback(async () => {
    try {
      const result = await getTicket(ticketId);
      setTicket(result.ticket);
      setMessages(result.messages);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('common.error_generic'));
    }
  }, [ticketId, t]);

  useEffect(() => {
    load();
  }, [load]);

  const onSend = async () => {
    if (!reply.trim()) return;

    setSending(true);
    setError(null);
    try {
      await addMessage(ticketId, reply.trim());
      setReply('');
      // Refetch rather than appending locally: the server may also have
      // moved the ticket's status (a reply on a resolved ticket reopens
      // it), and the header below should reflect that immediately.
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('common.error_generic'));
    } finally {
      setSending(false);
    }
  };

  const isClosed = ticket?.status === 'closed';

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {ticket && (
        <View style={styles.header}>
          <Text style={styles.subject}>{ticket.subject}</Text>
          <View style={styles.statusPill}>
            <Text style={styles.statusLabel}>{t(`support.status_${ticket.status}`)}</Text>
          </View>
        </View>
      )}

      {messages.map((item) => {
        const fromSupport = item.senderType === 'admin';
        return (
          <View
            key={item.id}
            style={[styles.bubble, fromSupport ? styles.fromSupport : styles.fromUser]}
          >
            <Text style={[styles.sender, fromSupport && styles.senderSupport]}>
              {fromSupport ? t('support.sender_support') : t('support.sender_you')}
            </Text>
            <Text style={styles.body}>{item.body}</Text>
            <Text style={styles.time}>{formatDateTime(item.createdAt)}</Text>
          </View>
        );
      })}

      {error && <InfoNote tone="danger">{error}</InfoNote>}

      {isClosed ? (
        <View style={styles.closed}>
          <InfoNote>{t('support.closed_notice')}</InfoNote>
        </View>
      ) : (
        <View style={styles.replyBlock}>
          <Field
            label={t('support.reply_label')}
            value={reply}
            onChangeText={setReply}
            multiline
            numberOfLines={4}
            textAlignVertical="top"
            style={styles.textArea}
          />
          <PrimaryButton
            icon="send"
            label={t('support.reply_button')}
            onPress={onSend}
            loading={sending}
            disabled={!reply.trim()}
          />
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.ground },
  content: { padding: spacing.lg, paddingBottom: spacing.xl },
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, marginBottom: spacing.lg },
  subject: { flex: 1, fontSize: typography.heading, fontWeight: '700', color: colors.text, letterSpacing: -0.2 },
  statusPill: {
    backgroundColor: colors.surfaceAlt,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.sm + 2,
    paddingVertical: 5,
  },
  statusLabel: { fontSize: 11, fontWeight: '600', color: colors.muted },
  // Le fil respecte la convention universelle des messageries : ce que
  // l'utilisateur a écrit à droite, la réponse du support à gauche. Sur
  // un écran d'aide, reconnaître qui parle doit être immédiat.
  bubble: { borderRadius: radius.md, padding: spacing.md - 2, marginBottom: spacing.sm + 2, maxWidth: '90%' },
  fromUser: { backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, alignSelf: 'flex-end' },
  fromSupport: { backgroundColor: colors.primarySoft, alignSelf: 'flex-start' },
  sender: { fontSize: typography.overline, fontWeight: '600', color: colors.muted, marginBottom: spacing.xs },
  senderSupport: { color: colors.primary },
  body: { fontSize: typography.label, color: colors.text, lineHeight: 23 },
  time: { fontSize: 11, color: colors.muted, marginTop: spacing.sm - 2 },
  closed: { marginTop: spacing.md },
  replyBlock: { marginTop: spacing.lg },
  textArea: { minHeight: 110, paddingTop: spacing.md, lineHeight: typography.body + 7 },
});
