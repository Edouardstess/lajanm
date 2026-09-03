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

// --- Support tickets ---

export type TicketCategory = 'general' | 'transaction' | 'kyc' | 'technical' | 'other';
export type TicketStatus = 'open' | 'in_progress' | 'resolved' | 'closed';

export interface SupportTicket {
  id: string;
  userId: string;
  subject: string;
  category: TicketCategory;
  status: TicketStatus;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SupportMessage {
  id: string;
  ticketId: string;
  senderId: string;
  senderType: 'user' | 'admin';
  body: string;
  createdAt: string;
}

export function getTicketQueue(status?: TicketStatus) {
  const query = status ? `?status=${status}` : '';
  return apiRequest<SupportTicket[]>(`/support/tickets/queue${query}`, { authenticated: true });
}

export function getTicket(id: string) {
  return apiRequest<{ ticket: SupportTicket; messages: SupportMessage[] }>(
    `/support/tickets/queue/${id}`,
    { authenticated: true },
  );
}

export function replyToTicket(id: string, body: string) {
  return apiRequest<SupportMessage>(`/support/tickets/${id}/reply`, {
    method: 'POST',
    authenticated: true,
    body: { body },
  });
}

export function updateTicket(id: string, fields: { status?: TicketStatus }) {
  return apiRequest<SupportTicket>(`/support/tickets/${id}`, {
    method: 'PATCH',
    authenticated: true,
    body: fields,
  });
}

// --- FAQ content ---

export interface FaqEntry {
  id: string;
  category: string;
  question: string;
  answer: string;
  sortOrder: number;
  isPublished: boolean;
}

export function getAllFaqs() {
  return apiRequest<FaqEntry[]>('/support/faq/all', { authenticated: true });
}

export function createFaq(fields: {
  category: string;
  question: string;
  answer: string;
  sortOrder?: number;
  isPublished?: boolean;
}) {
  return apiRequest<FaqEntry>('/support/faq', { method: 'POST', authenticated: true, body: fields });
}

export function updateFaq(id: string, fields: Partial<Omit<FaqEntry, 'id'>>) {
  return apiRequest<FaqEntry>(`/support/faq/${id}`, {
    method: 'PATCH',
    authenticated: true,
    body: fields,
  });
}

export function deleteFaq(id: string) {
  return apiRequest<{ deleted: true }>(`/support/faq/${id}`, { method: 'DELETE', authenticated: true });
}
