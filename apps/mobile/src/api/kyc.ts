import { apiRequest } from './client';

export type KycStatus = 'pending' | 'approved' | 'rejected';

export interface KycSubmission {
  id: string;
  status: KycStatus;
  rejectionReason: string | null;
  createdAt: string;
}

export function submitKyc(idDocumentUrl: string, selfieUrl: string) {
  return apiRequest<KycSubmission>('/kyc/submissions', {
    method: 'POST',
    authenticated: true,
    body: { idDocumentUrl, selfieUrl },
  });
}

export function myKycSubmissions() {
  return apiRequest<KycSubmission[]>('/kyc/submissions/me', { authenticated: true });
}
