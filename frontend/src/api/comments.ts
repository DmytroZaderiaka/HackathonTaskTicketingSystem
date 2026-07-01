import { api } from './client';

export interface Comment {
  id: string;
  ticketId: string;
  author: { id: string; email: string };
  body: string;
  createdAt: string;
}

export const commentsApi = {
  list: (ticketId: string) => api.get<Comment[]>(`/tickets/${ticketId}/comments`),
  add: (ticketId: string, body: string) => api.post<Comment>(`/tickets/${ticketId}/comments`, { body }),
};
