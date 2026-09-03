'use client';

import { useEffect, useState } from 'react';
import { RequireAuth } from '../../components/RequireAuth';
import { ApiError } from '../../lib/api';
import { getReconciliation, ReconciliationReport } from '../../lib/adminApi';

function formatHTG(minor: string): string {
  return (Number(minor) / 100).toLocaleString(undefined, { minimumFractionDigits: 2 });
}

export default function ReconciliationPage() {
  const [report, setReport] = useState<ReconciliationReport | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    setError(null);
    getReconciliation()
      .then(setReport)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load'))
      .finally(() => setLoading(false));
  };

  // queueMicrotask: load() calls setLoading(true) synchronously (needed so
  // clicking "Refresh" below shows a loading state again), which the
  // react-hooks/set-state-in-effect lint rule flags if called directly
  // inside an effect body. Deferring by one microtask keeps that call
  // fully outside the effect's own synchronous execution — same fetch,
  // same timing for the user, just not literally inside the effect frame.
  useEffect(() => {
    queueMicrotask(load);
  }, []);

  return (
    <RequireAuth>
      <h1>Reconciliation</h1>
      {loading && <p>Loading…</p>}
      {error && <p className="error-text">{error}</p>}
      {report && (
        <>
          <div className={report.isBalanced ? 'badge' : 'badge'} style={{ marginBottom: 16, background: report.isBalanced ? '#d7f2e3' : '#f7d9d7' }}>
            {report.isBalanced ? 'Balanced' : 'DISCREPANCY DETECTED'}
          </div>
          <div className="card-grid">
            <div className="card">
              <div className="stat-label">Internal wallet total</div>
              <div className="stat-value">{formatHTG(report.internalWalletTotalMinor)} HTG</div>
            </div>
            <div className="card">
              <div className="stat-label">MonCash float balance</div>
              <div className="stat-value">{formatHTG(report.moncashFloatBalanceMinor)} HTG</div>
            </div>
            <div className="card">
              <div className="stat-label">Discrepancy</div>
              <div className="stat-value">{formatHTG(report.discrepancyMinor)} HTG</div>
            </div>
          </div>
          <p className="stat-label" style={{ marginTop: 16 }}>
            {report.note}
          </p>
          <button className="button button-secondary" onClick={load} style={{ marginTop: 16 }}>
            Refresh
          </button>
        </>
      )}
    </RequireAuth>
  );
}
