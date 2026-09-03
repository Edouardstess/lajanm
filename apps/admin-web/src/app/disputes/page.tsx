'use client';

import { useEffect, useState } from 'react';
import { RequireAuth } from '../../components/RequireAuth';
import { ApiError } from '../../lib/api';
import { Dispute, DisputeStatus, getDisputes, updateDispute } from '../../lib/adminApi';

const STATUSES: DisputeStatus[] = ['open', 'investigating', 'resolved', 'rejected'];

export default function DisputesPage() {
  const [disputes, setDisputes] = useState<Dispute[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [notesDraft, setNotesDraft] = useState<Record<string, string>>({});

  const load = () => {
    setLoading(true);
    getDisputes()
      .then(setDisputes)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load'))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    queueMicrotask(load);
  }, []);

  const changeStatus = async (id: string, status: DisputeStatus) => {
    setBusyId(id);
    try {
      const updated = await updateDispute(id, { status });
      setDisputes((prev) => prev.map((d) => (d.id === id ? updated : d)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to update dispute');
    } finally {
      setBusyId(null);
    }
  };

  const saveNotes = async (id: string) => {
    setBusyId(id);
    try {
      const updated = await updateDispute(id, { internalNotes: notesDraft[id] ?? '' });
      setDisputes((prev) => prev.map((d) => (d.id === id ? updated : d)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to save notes');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <RequireAuth>
      <h1>Disputes</h1>
      {loading && <p>Loading…</p>}
      {error && <p className="error-text">{error}</p>}
      {!loading && disputes.length === 0 && <p className="stat-label">No disputes filed.</p>}
      {disputes.map((d) => (
        <div className="card" key={d.id}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
            <strong>{d.subject}</strong>
            <span className="badge">{d.status}</span>
          </div>
          <p style={{ marginBottom: 8 }}>{d.description}</p>
          <p className="stat-label" style={{ marginBottom: 12 }}>
            User {d.userId} · {new Date(d.createdAt).toLocaleString()}
          </p>

          <label htmlFor={`notes-${d.id}`}>Internal notes</label>
          <textarea
            id={`notes-${d.id}`}
            rows={3}
            defaultValue={d.internalNotes ?? ''}
            onChange={(e) => setNotesDraft((prev) => ({ ...prev, [d.id]: e.target.value }))}
          />
          <button className="button button-secondary" disabled={busyId === d.id} onClick={() => saveNotes(d.id)}>
            Save notes
          </button>

          <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
            {STATUSES.filter((s) => s !== d.status).map((s) => (
              <button
                key={s}
                className="button button-secondary"
                disabled={busyId === d.id}
                onClick={() => changeStatus(d.id, s)}
              >
                Mark {s}
              </button>
            ))}
          </div>
        </div>
      ))}
    </RequireAuth>
  );
}
