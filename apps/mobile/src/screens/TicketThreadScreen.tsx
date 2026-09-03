import { useRoute } from '@react-navigation/native';
import React, { useCallback, useEffect, useState } from 'react';
import { ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { ApiError } from '../api/client';
import { addMessage, getTicket, SupportMessage, SupportTicket } from '../api/support';
import { PrimaryButton } from '../components/PrimaryButton';
import { useTranslation } from '../i18n';
import { colors, spacing, touchTarget, typography } from '../theme';

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
        <>
          <Text style={styles.subject}>{ticket.subject}</Text>
          <Text style={styles.status}>{t(`support.status_${ticket.status}`)}</Text>
        </>
      )}

      {messages.map((item) => {
        const fromSupport = item.senderType === 'admin';
        return (
          <View key={item.id} style={[styles.bubble, fromSupport ? styles.fromSupport : styles.fromUser]}>
            <Text style={styles.sender}>
              {fromSupport ? t('support.sender_support') : t('support.sender_you')}
            </Text>
            <Text style={styles.body}>{item.body}</Text>
            <Text style={styles.time}>{new Date(item.createdAt).toLocaleString()}</Text>
          </View>
        );
      })}

      {error && <Text style={styles.errorText}>{error}</Text>}

      {isClosed ? (
        <Text style={styles.closedNotice}>{t('support.closed_notice')}</Text>
      ) : (
        <>
          <Text style={styles.label}>{t('support.reply_label')}</Text>
          <TextInput
            style={[styles.input, styles.textArea]}
            value={reply}
            onChangeText={setReply}
            multiline
            numberOfLines={4}
            textAlignVertical="top"
          />
          <PrimaryButton
            label={t('support.reply_button')}
            onPress={onSend}
            loading={sending}
            disabled={!reply.trim()}
          />
        </>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background },
  content: { padding: spacing.lg },
  subject: { fontSize: typography.title, fontWeight: '700', color: colors.text },
  status: { fontSize: typography.label, color: colors.muted, marginBottom: spacing.lg },
  bubble: { borderRadius: 12, padding: spacing.md, marginBottom: spacing.md },
  fromUser: { backgroundColor: '#EFEFEF', alignSelf: 'flex-end', maxWidth: '90%' },
  fromSupport: { backgroundColor: '#E4F1EC', alignSelf: 'flex-start', maxWidth: '90%' },
  sender: { fontSize: typography.label, color: colors.muted, marginBottom: spacing.xs },
  body: { fontSize: typography.body, color: colors.text, lineHeight: 24 },
  time: { fontSize: typography.label, color: colors.muted, marginTop: spacing.xs },
  label: { fontSize: typography.label, color: colors.text, marginBottom: spacing.xs, marginTop: spacing.md },
  input: {
    minHeight: touchTarget.minHeight,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    paddingHorizontal: spacing.md,
    fontSize: typography.body,
    color: colors.text,
  },
  textArea: { minHeight: 100, paddingTop: spacing.md },
  closedNotice: { fontSize: typography.body, color: colors.muted, marginTop: spacing.md, lineHeight: 24 },
  errorText: { color: colors.danger, marginTop: spacing.md, textAlign: 'center' },
});
