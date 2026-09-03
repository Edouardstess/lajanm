'use client';

import { useEffect, useState } from 'react';
import { RequireAuth } from '../../components/RequireAuth';
import { ApiError } from '../../lib/api';
import {
  getTicket,
  getTicketQueue,
  replyToTicket,
  SupportMessage,
  SupportTicket,
  TicketStatus,
  updateTicket,
} from '../../lib/adminApi';

const STATUSES: TicketStatus[] = ['open', 'in_progress', 'resolved', 'closed'];

export default function TicketsPage() {
  const [tickets, setTickets] = useState<SupportTicket[]>([]);
  const [filter, setFilter] = useState<TicketStatus | 'all'>('all');
  const [openId, setOpenId] = useState<string | null>(null);
  const [thread, setThread] = useState<SupportMessage[]>([]);
  const [replyDraft, setReplyDraft] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const load = (status: TicketStatus | 'all' = filter) => {
    setLoading(true);
    setError(null);
    getTicketQueue(status === 'all' ? undefined : status)
      .then(setTickets)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load'))
      .finally(() => setLoading(false));
  };

  // queueMicrotask: load() sets state synchronously, which the
  // react-hooks/set-state-in-effect rule flags inside an effect body.
  useEffect(() => {
    queueMicrotask(load);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const applyFilter = (status: TicketStatus | 'all') => {
    setFilter(status);
    setOpenId(null);
    load(status);
  };

  const openThread = async (id: string) => {
    if (openId === id) {
      setOpenId(null);
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const result = await getTicket(id);
      setThread(result.messages);
      setOpenId(id);
      setReplyDraft('');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to load thread');
    } finally {
      setBusy(false);
    }
  };

  const sendReply = async (id: string) => {
    if (!replyDraft.trim()) return;
    setBusy(true);
    try {
      await replyToTicket(id, replyDraft.trim());
      setReplyDraft('');
      // Refetch both: replying can move an open ticket to in_progress
      // server-side, so the row's badge would otherwise go stale.
      const result = await getTicket(id);
      setThread(result.messages);
      setTickets((prev) => prev.map((t) => (t.id === id ? result.ticket : t)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to send reply');
    } finally {
      setBusy(false);
    }
  };

  const changeStatus = async (id: string, status: TicketStatus) => {
    setBusy(true);
    try {
      const updated = await updateTicket(id, { status });
      setTickets((prev) => prev.map((t) => (t.id === id ? updated : t)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to update ticket');
    } finally {
      setBusy(false);
    }
  };

  return (
    <RequireAuth>
      <h1>Support tickets</h1>

      <div style={{ display: 'flex', gap: 8, marginBottom: 16, flexWrap: 'wrap' }}>
        {(['all', ...STATUSES] as const).map((s) => (
          <button
            key={s}
            className={filter === s ? 'button' : 'button button-secondary'}
            onClick={() => applyFilter(s)}
          >
            {s === 'all' ? 'All' : s}
          </button>
        ))}
      </div>

      {loading && <p>Loading…</p>}
      {error && <p className="error-text">{error}</p>}
      {!loading && tickets.length === 0 && <p className="stat-label">No tickets in this view.</p>}

      {tickets.map((t) => (
        <div className="card" key={t.id}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
            <strong>{t.subject}</strong>
            <span className="badge">{t.status}</span>
          </div>
          <p className="stat-label" style={{ marginBottom: 12 }}>
            {t.category} · user {t.userId} · updated {new Date(t.updatedAt).toLocaleString()}
          </p>

          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            <button className="button button-secondary" disabled={busy} onClick={() => openThread(t.id)}>
              {openId === t.id ? 'Hide conversation' : 'Open conversation'}
            </button>
            {STATUSES.filter((s) => s !== t.status).map((s) => (
              <button
                key={s}
                className="button button-secondary"
                disabled={busy}
                onClick={() => changeStatus(t.id, s)}
              >
                Mark {s}
              </button>
            ))}
          </div>

          {openId === t.id && (
            <div style={{ marginTop: 16 }}>
              {thread.map((m) => (
                <div
                  key={m.id}
                  style={{
                    borderLeft: `3px solid ${m.senderType === 'admin' ? '#0f6e4f' : '#d0d0d0'}`,
                    paddingLeft: 12,
                    marginBottom: 12,
                  }}
                >
                  <div className="stat-label">
                    {m.senderType === 'admin' ? 'Support' : 'Customer'} ·{' '}
                    {new Date(m.createdAt).toLocaleString()}
                  </div>
                  <div style={{ whiteSpace: 'pre-wrap' }}>{m.body}</div>
                </div>
              ))}

              <label htmlFor={`reply-${t.id}`}>Reply</label>
              <textarea
                id={`reply-${t.id}`}
                rows={3}
                value={replyDraft}
                onChange={(e) => setReplyDraft(e.target.value)}
              />
              <button className="button" disabled={busy || !replyDraft.trim()} onClick={() => sendReply(t.id)}>
                Send reply
              </button>
            </div>
          )}
        </div>
      ))}
    </RequireAuth>
  );
}
