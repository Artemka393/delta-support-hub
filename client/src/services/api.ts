import type { CreateTicketRequest, DashboardSummary, KnowledgeArticle, Ticket, TicketClassification } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7180';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers
    },
    ...init
  });

  if (!response.ok) {
    throw new Error(`API request failed: ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export const supportApi = {
  getSummary: () => request<DashboardSummary>('/api/dashboard/summary'),
  getTickets: () => request<Ticket[]>('/api/tickets'),
  getKnowledge: () => request<KnowledgeArticle[]>('/api/knowledge'),
  createTicket: (payload: CreateTicketRequest) =>
    request<Ticket>('/api/tickets', {
      method: 'POST',
      body: JSON.stringify(payload)
    }),
  classify: (payload: CreateTicketRequest) =>
    request<TicketClassification>('/api/automation/classify', {
      method: 'POST',
      body: JSON.stringify(payload)
    }),
  updateStatus: (id: number, status: Ticket['status']) =>
    request<Ticket>(`/api/tickets/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status })
    })
};
