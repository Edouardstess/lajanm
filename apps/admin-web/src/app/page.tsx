'use client';

import Link from 'next/link';
import { RequireAuth } from '../components/RequireAuth';
import { useAdminAuth } from '../lib/AdminAuthContext';

const sections = [
  { href: '/reconciliation', label: 'Reconciliation', description: 'Internal ledger vs. MonCash float' },
  { href: '/kyc', label: 'KYC queue', description: 'Review pending identity verifications' },
  { href: '/fraud', label: 'Fraud flags', description: 'Triage velocity-rule hits' },
  { href: '/disputes', label: 'Disputes', description: 'Customer-filed transaction complaints' },
  { href: '/tickets', label: 'Support tickets', description: 'Answer customer help requests' },
  { href: '/faq', label: 'FAQ content', description: 'Publish answers shown in the mobile app' },
  { href: '/sar', label: 'Suspicious activity reports', description: 'Internal UCREF-filing documentation' },
];

export default function Home() {
  const { admin } = useAdminAuth();

  return (
    <RequireAuth>
      <h1>Welcome, {admin?.fullName ?? admin?.email}</h1>
      <div className="card-grid">
        {sections.map((s) => (
          <Link key={s.href} href={s.href} className="card">
            <div style={{ fontWeight: 700, marginBottom: 4 }}>{s.label}</div>
            <div className="stat-label">{s.description}</div>
          </Link>
        ))}
      </div>
    </RequireAuth>
  );
}
