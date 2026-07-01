import { api } from './client';

export type TicketType = 'bug' | 'feature' | 'fix';
export type TicketState =
  | 'new'
  | 'ready_for_implementation'
  | 'in_progress'
  | 'ready_for_acceptance'
  | 'done';

export interface Ticket {
  id: string;
  teamId: string;
  epicId: string | null;
  type: TicketType;
  state: TicketState;
  title: string;
  body: string;
  createdBy: { id: string; email: string };
  createdAt: string;
  modifiedAt: string;
}

export interface TicketInput {
  teamId: string;
  epicId: string | null;
  type: TicketType;
  state: TicketState;
  title: string;
  body: string;
}

export interface TicketFilters {
  type?: TicketType;
  epicId?: string;
  search?: string;
}

export const TICKET_TYPES: TicketType[] = ['bug', 'feature', 'fix'];

export const TICKET_STATES: TicketState[] = [
  'new',
  'ready_for_implementation',
  'in_progress',
  'ready_for_acceptance',
  'done',
];

/** Human-readable label for a canonical state value, e.g. "Ready For Implementation". */
export function stateLabel(state: TicketState): string {
  return state
    .split('_')
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
}

export const ticketsApi = {
  list: (teamId: string, filters: TicketFilters = {}) => {
    const params = new URLSearchParams({ teamId });
    if (filters.type) params.set('type', filters.type);
    if (filters.epicId) params.set('epicId', filters.epicId);
    if (filters.search) params.set('search', filters.search);
    return api.get<Ticket[]>(`/tickets?${params.toString()}`);
  },
  get: (id: string) => api.get<Ticket>(`/tickets/${id}`),
  create: (input: TicketInput) => api.post<Ticket>('/tickets', input),
  update: (id: string, input: TicketInput) => api.put<Ticket>(`/tickets/${id}`, input),
  remove: (id: string) => api.del<void>(`/tickets/${id}`),
};
