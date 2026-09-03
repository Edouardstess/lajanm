import { apiRequest } from './api';

export type AdminRole = 'operator' | 'admin';

export interface Admin {
  id: string;
  email: string;
  role: AdminRole;
  fullName: string | null;
}

export function login(email: string, password: string) {
  return apiRequest<{ accessToken: string; admin: Admin }>('/admin/auth/login', {
    method: 'POST',
    body: { email, password },
  });
}

// --- Reconciliation ---

export interface ReconciliationReport {
  internalWalletTotalMinor: string;
  moncashFloatBalanceMinor: string;
  discrepancyMinor: string;
  isBalanced: boolean;
  note: string;
}

export function getReconciliation() {
  return apiRequest<ReconciliationReport>('/compliance/reconciliation', { authenticated: true });
}

// --- KYC ---

export type KycStatus = 'pending' | 'approved' | 'rejected';

export interface KycSubmission {
  id: string;
  userId: string;
  idDocumentUrl: string;
  selfieUrl: string;
  status: KycStatus;
  createdAt: string;
}

export function getKycQueue() {
  return apiRequest<KycSubmission[]>('/kyc/submissions/queue', { authenticated: true });
}

export function decideKyc(id: string, decision: 'approved' | 'rejected', rejectionReason?: string) {
  return apiRequest<KycSubmission>(`/kyc/submissions/${id}/decision`, {
    method: 'PATCH',
    authenticated: true,
    body: { decision, rejectionReason },
  });
}

// --- Fraud ---

export type FraudFlagStatus = 'open' | 'resolved' | 'confirmed_suspect' | 'false_positive';

export interface FraudFlag {
  id: string;
  userId: string;
  ruleCode: string;
  status: FraudFlagStatus;
  relatedOperationId: string | null;
  details: Record<string, unknown> | null;
  createdAt: string;
}

export function getFraudFlags() {
  return apiRequest<FraudFlag[]>('/fraud/flags', { authenticated: true });
}

export function resolveFraudFlag(id: string, status: FraudFlagStatus) {
  return apiRequest<FraudFlag>(`/fraud/flags/${id}`, {
    method: 'PATCH',
    authenticated: true,
    body: { status },
  });
}

// --- Disputes ---

export type DisputeStatus = 'open' | 'investigating' | 'resolved' | 'rejected';

export interface Dispute {
  id: string;
  userId: string;
  subject: string;
  description: string;
  status: DisputeStatus;
  internalNotes: string | null;
  relatedOperationId: string | null;
  createdAt: string;
}

export function getDisputes() {
  return apiRequest<Dispute[]>('/compliance/disputes', { authenticated: true });
}

export function updateDispute(id: string, fields: { status?: DisputeStatus; internalNotes?: string }) {
  return apiRequest<Dispute>(`/compliance/disputes/${id}`, {
    method: 'PATCH',
    authenticated: true,
    body: fields,
  });
}

// --- Suspicious activity reports ---

export interface SuspiciousActivityReport {
  id: string;
  subjectUserId: string;
  relatedOperationIds: string[];
  reason: string;
  filedBy: string;
  createdAt: string;
}

export function getSars() {
  return apiRequest<SuspiciousActivityReport[]>('/compliance/sar', { authenticated: true });
}

export function createSar(subjectUserId: string, relatedOperationIds: string[], reason: string) {
  return apiRequest<SuspiciousActivityReport>('/compliance/sar', {
    method: 'POST',
    authenticated: true,
    body: { subjectUserId, relatedOperationIds, reason },
  });
}
