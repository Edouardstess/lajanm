'use client';

import { useRouter } from 'next/navigation';
import React, { useEffect } from 'react';
import { useAdminAuth } from '../lib/AdminAuthContext';
import { Nav } from './Nav';

export function RequireAuth({ children }: { children: React.ReactNode }) {
  const { admin, isReady } = useAdminAuth();
  const router = useRouter();

  useEffect(() => {
    if (isReady && !admin) {
      router.replace('/login');
    }
  }, [isReady, admin, router]);

  if (!isReady || !admin) return null;

  return (
    <>
      <Nav />
      <main className="page">{children}</main>
    </>
  );
}
