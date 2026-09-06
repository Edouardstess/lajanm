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

export function transfer(
  recipientPhone: string,
  amountHTG: number,
  clientRequestId: string,
  otp?: { otpRequestId: string; otpCode: string },
) {
  return apiRequest<{ operationId: string; idempotent: boolean }>('/wallet/transfer', {
    method: 'POST',
    authenticated: true,
    body: { recipientPhone, amountHTG, clientRequestId, ...otp },
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

export interface RecipientLookup {
  exists: boolean;
  /** Nom masqué (« Mirlande P. »), ou null si le compte n'en a pas. */
  displayName: string | null;
  /** Numéro sous sa forme canonique, tel que le serveur l'a compris. */
  phone: string;
}

/**
 * Dit à qui appartient un numéro, avant l'envoi.
 *
 * Un transfert ne se rattrape pas : sans cette étape, un chiffre de trop
 * envoie l'argent chez un inconnu et on l'apprend après.
 */
export function lookupRecipient(phone: string) {
  return apiRequest<RecipientLookup>(
    `/wallet/recipients/lookup?phone=${encodeURIComponent(phone)}`,
    { authenticated: true },
  );
}
