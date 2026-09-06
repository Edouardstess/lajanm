import { apiRequest } from './client';

export type KycStatus = 'pending' | 'approved' | 'rejected';

export interface KycSubmission {
  id: string;
  status: KycStatus;
  rejectionReason: string | null;
  createdAt: string;
}

/**
 * Les deux identifiants viennent de `uploadKycPhoto` : le serveur n'accepte
 * plus de chaîne libre, seulement des fichiers qu'il a lui-même inspectés.
 */
export function submitKyc(idDocumentFileId: string, selfieFileId: string) {
  return apiRequest<KycSubmission>('/kyc/submissions', {
    method: 'POST',
    authenticated: true,
    body: { idDocumentFileId, selfieFileId },
  });
}

export function myKycSubmissions() {
  return apiRequest<KycSubmission[]>('/kyc/submissions/me', { authenticated: true });
}
