import { apiRequest } from './client';

export type OtpPurpose = 'transfer' | 'payout';

export function requestOtp(purpose: OtpPurpose) {
  return apiRequest<{ otpRequestId: string; expiresAt: string }>('/security/otp/request', {
    method: 'POST',
    authenticated: true,
    body: { purpose },
  });
}
