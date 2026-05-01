export type Priority = 'Low' | 'Medium' | 'High' | 'Critical';
export type TicketStatus = 'Open' | 'InProgress' | 'WaitingForUser' | 'Resolved';

export interface Ticket {
  id: number;
  title: string;
  requester: string;
  system: string;
  description: string;
  priority: Priority;
  status: TicketStatus;
  assignee: string;
  tags: string[];
  automationHint: string;
  createdAt: string;
  dueAt: string;
}

export interface CreateTicketRequest {
  title: string;
  requester: string;
  system: string;
  description: string;
}

export interface TicketClassification {
  priority: Priority;
  assignee: string;
  tags: string[];
  automationHint: string;
  suggestedReply: string;
}

export interface KnowledgeArticle {
  id: number;
  title: string;
  system: string;
  problem: string;
  resolution: string;
  tags: string[];
}

export interface DashboardSummary {
  openCount: number;
  inProgressCount: number;
  resolvedTodayCount: number;
  overdueCount: number;
  automationCoveragePercent: number;
}
