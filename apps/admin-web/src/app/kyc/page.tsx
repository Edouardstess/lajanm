'use client';

import { useEffect, useState } from 'react';
import { RequireAuth } from '../../components/RequireAuth';
import { ApiError } from '../../lib/api';
import { decideKyc, getKycQueue, KycSubmission } from '../../lib/adminApi';

export default function KycQueuePage() {
  const [submissions, setSubmissions] = useState<KycSubmission[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = () => {
    setLoading(true);
    getKycQueue()
      .then(setSubmissions)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load'))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    queueMicrotask(load);
  }, []);

  const decide = async (id: string, decision: 'approved' | 'rejected') => {
    const rejectionReason =
      decision === 'rejected' ? window.prompt('Reason for rejection (shown internally):') ?? undefined : undefined;
    setBusyId(id);
    try {
      await decideKyc(id, decision, rejectionReason);
      setSubmissions((prev) => prev.filter((s) => s.id !== id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to submit decision');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <RequireAuth>
      <h1>KYC review queue</h1>
      {loading && <p>Loading…</p>}
      {error && <p className="error-text">{error}</p>}
      {!loading && submissions.length === 0 && <p className="stat-label">No pending submissions.</p>}
      {submissions.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>User</th>
              <th>ID document</th>
              <th>Selfie</th>
              <th>Submitted</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {submissions.map((s) => (
              <tr key={s.id}>
                <td>{s.userId}</td>
                <td>
                  <a href={s.idDocumentUrl} target="_blank" rel="noreferrer">
                    view
                  </a>
                </td>
                <td>
                  <a href={s.selfieUrl} target="_blank" rel="noreferrer">
                    view
                  </a>
                </td>
                <td>{new Date(s.createdAt).toLocaleString()}</td>
                <td style={{ display: 'flex', gap: 8 }}>
                  <button
                    className="button"
                    disabled={busyId === s.id}
                    onClick={() => decide(s.id, 'approved')}
                  >
                    Approve
                  </button>
                  <button
                    className="button button-danger"
                    disabled={busyId === s.id}
                    onClick={() => decide(s.id, 'rejected')}
                  >
                    Reject
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </RequireAuth>
  );
}
