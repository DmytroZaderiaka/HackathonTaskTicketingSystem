import { api } from './client';

export interface Epic {
  id: string;
  teamId: string;
  title: string;
  description: string | null;
  createdAt: string;
  modifiedAt: string;
}

export interface EpicInput {
  title: string;
  description: string | null;
}

export const epicsApi = {
  list: (teamId: string) => api.get<Epic[]>(`/epics?teamId=${encodeURIComponent(teamId)}`),
  create: (teamId: string, input: EpicInput) => api.post<Epic>('/epics', { teamId, ...input }),
  update: (id: string, input: EpicInput) => api.put<Epic>(`/epics/${id}`, input),
  remove: (id: string) => api.del<void>(`/epics/${id}`),
};
