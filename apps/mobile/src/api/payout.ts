import { apiRequest } from './client';

export type PayoutStatus = 'completed' | 'failed';

export interface PayoutResult {
  payoutTransactionId: string;
  status: PayoutStatus;
  failureReason: string | null;
}

export function getPayoutLimit() {
  return apiRequest<{ maxAmountHTG: number }>('/payout/limit', { authenticated: true });
}

export function initiatePayout(
  amountHTG: number,
  clientRequestId: string,
  otp?: { otpRequestId: string; otpCode: string },
) {
  return apiRequest<PayoutResult>('/payout/initiate', {
    method: 'POST',
    authenticated: true,
    body: { amountHTG, clientRequestId, ...otp },
  });
}
