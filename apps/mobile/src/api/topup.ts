import { apiRequest } from './client';

export type TopupStatus = 'pending' | 'completed' | 'failed';

export interface TopupTransaction {
  id: string;
  source: 'moncash' | 'natcash';
  amountMinor: string;
  currency: string;
  status: TopupStatus;
  failureReason: string | null;
  createdAt: string;
}

export interface InitiateTopupResult {
  transactionId: string;
  status: TopupStatus;
  gatewayUrl: string | null;
}

export function initiateTopup(amountHTG: number) {
  return apiRequest<InitiateTopupResult>('/topup/initiate', {
    method: 'POST',
    authenticated: true,
    body: { amountHTG },
  });
}

export function getTopupStatus(transactionId: string) {
  return apiRequest<TopupTransaction>(`/topup/${transactionId}`, { authenticated: true });
}

export function topupHistory() {
  return apiRequest<TopupTransaction[]>('/topup/history', { authenticated: true });
}
