import { apiRequest } from './client';

export interface BalanceSnapshot {
  balanceMinor: string;
  currency: string;
  asOf: string;
}

export type OperationType = 'topup' | 'payout' | 'transfer' | 'adjustment';
export type EntryDirection = 'credit' | 'debit';

export interface WalletHistoryEntry {
  id: string;
  operationType: OperationType;
  direction: EntryDirection;
  amountMinor: string;
  currency: string;
  createdAt: string;
}

export function getBalance() {
  return apiRequest<BalanceSnapshot>('/wallet/balance', { authenticated: true });
}

export function transfer(recipientPhone: string, amountHTG: number, clientRequestId: string) {
  return apiRequest<{ operationId: string; idempotent: boolean }>('/wallet/transfer', {
    method: 'POST',
    authenticated: true,
    body: { recipientPhone, amountHTG, clientRequestId },
  });
}

export function getHistory(filters: { type?: OperationType; limit?: number } = {}) {
  const params = new URLSearchParams();
  if (filters.type) params.set('type', filters.type);
  if (filters.limit) params.set('limit', String(filters.limit));
  const query = params.toString();
  return apiRequest<WalletHistoryEntry[]>(`/wallet/history${query ? `?${query}` : ''}`, {
    authenticated: true,
  });
}
