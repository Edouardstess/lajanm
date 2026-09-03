'use client';

import { FormEvent, useEffect, useState } from 'react';
import { RequireAuth } from '../../components/RequireAuth';
import { ApiError } from '../../lib/api';
import { createSar, getSars, SuspiciousActivityReport } from '../../lib/adminApi';

export default function SarPage() {
  const [sars, setSars] = useState<SuspiciousActivityReport[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const [subjectUserId, setSubjectUserId] = useState('');
  const [operationIds, setOperationIds] = useState('');
  const [reason, setReason] = useState('');

  const load = () => {
    setLoading(true);
    getSars()
      .then(setSars)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load'))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    queueMicrotask(load);
  }, []);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const ids = operationIds
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean);
      await createSar(subjectUserId, ids, reason);
      setSubjectUserId('');
      setOperationIds('');
      setReason('');
      load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to file report');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <RequireAuth>
      <h1>Suspicious activity reports</h1>
      <p className="stat-label" style={{ marginBottom: 16 }}>
        Internal case documentation only — this does not transmit anything to UCREF. A real filing is a
        separate, manual process using this record as working notes.
      </p>

      <form onSubmit={onSubmit} className="card">
        <label htmlFor="subjectUserId">Subject user ID</label>
        <input
          id="subjectUserId"
          value={subjectUserId}
          onChange={(e) => setSubjectUserId(e.target.value)}
          placeholder="uuid"
          required
        />
        <label htmlFor="operationIds">Related operation IDs (comma-separated)</label>
        <input
          id="operationIds"
          value={operationIds}
          onChange={(e) => setOperationIds(e.target.value)}
          placeholder="uuid, uuid, ..."
          required
        />
        <label htmlFor="reason">Reason</label>
        <textarea id="reason" rows={4} value={reason} onChange={(e) => setReason(e.target.value)} required />
        {error && <p className="error-text">{error}</p>}
        <button type="submit" className="button" disabled={submitting}>
          {submitting ? 'Filing…' : 'File report'}
        </button>
      </form>

      <h1 style={{ marginTop: 32 }}>Filed reports</h1>
      {loading && <p>Loading…</p>}
      {!loading && sars.length === 0 && <p className="stat-label">No reports filed.</p>}
      {sars.map((s) => (
        <div className="card" key={s.id}>
          <div className="stat-label">Subject: {s.subjectUserId}</div>
          <div className="stat-label">Operations: {s.relatedOperationIds.join(', ')}</div>
          <p style={{ marginTop: 8 }}>{s.reason}</p>
          <p className="stat-label" style={{ marginTop: 8 }}>
            Filed {new Date(s.createdAt).toLocaleString()}
          </p>
        </div>
      ))}
    </RequireAuth>
  );
}
