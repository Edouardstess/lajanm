'use client';

import { useEffect, useState } from 'react';
import { RequireAuth } from '../../components/RequireAuth';
import { ApiError } from '../../lib/api';
import { FraudFlag, FraudFlagStatus, getFraudFlags, resolveFraudFlag } from '../../lib/adminApi';

export default function FraudFlagsPage() {
  const [flags, setFlags] = useState<FraudFlag[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = () => {
    setLoading(true);
    getFraudFlags()
      .then(setFlags)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load'))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    queueMicrotask(load);
  }, []);

  const resolve = async (id: string, status: FraudFlagStatus) => {
    setBusyId(id);
    try {
      await resolveFraudFlag(id, status);
      setFlags((prev) => prev.filter((f) => f.id !== id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to resolve flag');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <RequireAuth>
      <h1>Open fraud flags</h1>
      {loading && <p>Loading…</p>}
      {error && <p className="error-text">{error}</p>}
      {!loading && flags.length === 0 && <p className="stat-label">No open flags.</p>}
      {flags.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>User</th>
              <th>Rule</th>
              <th>Details</th>
              <th>Raised</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {flags.map((f) => (
              <tr key={f.id}>
                <td>{f.userId}</td>
                <td>
                  <span className="badge">{f.ruleCode}</span>
                </td>
                <td>{f.details ? JSON.stringify(f.details) : '—'}</td>
                <td>{new Date(f.createdAt).toLocaleString()}</td>
                <td style={{ display: 'flex', gap: 8 }}>
                  <button className="button" disabled={busyId === f.id} onClick={() => resolve(f.id, 'resolved')}>
                    Resolved
                  </button>
                  <button
                    className="button button-danger"
                    disabled={busyId === f.id}
                    onClick={() => resolve(f.id, 'confirmed_suspect')}
                  >
                    Confirm suspect
                  </button>
                  <button
                    className="button button-secondary"
                    disabled={busyId === f.id}
                    onClick={() => resolve(f.id, 'false_positive')}
                  >
                    False positive
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
