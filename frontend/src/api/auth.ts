import { api } from './client';

export interface CurrentUser {
  id: string;
  email: string;
}

export const authApi = {
  signup: (email: string, password: string) => api.post<void>('/auth/signup', { email, password }),
  login: (email: string, password: string) => api.post<CurrentUser>('/auth/login', { email, password }),
  logout: () => api.post<void>('/auth/logout'),
  me: () => api.get<CurrentUser>('/auth/me'),
  resend: (email: string) => api.post<void>('/auth/resend', { email }),
  verifyEmail: (token: string) =>
    api.get<{ message: string }>(`/auth/verify-email?token=${encodeURIComponent(token)}`),
};
