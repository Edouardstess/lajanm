import { apiRequest } from './client';

export type TicketCategory = 'general' | 'transaction' | 'kyc' | 'technical' | 'other';
export type TicketStatus = 'open' | 'in_progress' | 'resolved' | 'closed';

export interface SupportTicket {
  id: string;
  subject: string;
  category: TicketCategory;
  status: TicketStatus;
  createdAt: string;
  updatedAt: string;
}

export interface SupportMessage {
  id: string;
  ticketId: string;
  senderType: 'user' | 'admin';
  body: string;
  createdAt: string;
}

export interface TicketWithThread {
  ticket: SupportTicket;
  messages: SupportMessage[];
}

export interface FaqEntry {
  id: string;
  category: string;
  question: string;
  answer: string;
  sortOrder: number;
}

export function listFaq() {
  return apiRequest<FaqEntry[]>('/support/faq', { authenticated: true });
}

export function listMyTickets() {
  return apiRequest<SupportTicket[]>('/support/tickets/me', { authenticated: true });
}

export function getTicket(ticketId: string) {
  return apiRequest<TicketWithThread>(`/support/tickets/${ticketId}`, { authenticated: true });
}

export function createTicket(subject: string, message: string, category?: TicketCategory) {
  return apiRequest<TicketWithThread>('/support/tickets', {
    method: 'POST',
    authenticated: true,
    body: { subject, message, ...(category ? { category } : {}) },
  });
}

export function addMessage(ticketId: string, body: string) {
  return apiRequest<SupportMessage>(`/support/tickets/${ticketId}/messages`, {
    method: 'POST',
    authenticated: true,
    body: { body },
  });
}
