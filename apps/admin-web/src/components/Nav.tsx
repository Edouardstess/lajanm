'use client';

import Link from 'next/link';
import { useAdminAuth } from '../lib/AdminAuthContext';

const links = [
  { href: '/', label: 'Dashboard' },
  { href: '/reconciliation', label: 'Reconciliation' },
  { href: '/kyc', label: 'KYC queue' },
  { href: '/fraud', label: 'Fraud flags' },
  { href: '/disputes', label: 'Disputes' },
  { href: '/tickets', label: 'Tickets' },
  { href: '/faq', label: 'FAQ' },
  { href: '/sar', label: 'SAR' },
];

export function Nav() {
  const { admin, logout } = useAdminAuth();

  return (
    <nav className="nav">
      <div className="nav-links">
        {links.map((link) => (
          <Link key={link.href} href={link.href} className="nav-link">
            {link.label}
          </Link>
        ))}
      </div>
      <div className="nav-user">
        <span>
          {admin?.email} ({admin?.role})
        </span>
        <button onClick={logout} className="button button-secondary">
          Log out
        </button>
      </div>
    </nav>
  );
}
