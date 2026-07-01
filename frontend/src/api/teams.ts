import { api } from './client';

export interface Team {
  id: string;
  name: string;
  createdAt: string;
  modifiedAt: string;
}

export const teamsApi = {
  list: () => api.get<Team[]>('/teams'),
  create: (name: string) => api.post<Team>('/teams', { name }),
  rename: (id: string, name: string) => api.put<Team>(`/teams/${id}`, { name }),
  remove: (id: string) => api.del<void>(`/teams/${id}`),
};
