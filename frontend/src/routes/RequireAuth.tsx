import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

/**
 * Gate for authenticated routes: shows nothing while the session is being resolved,
 * then either renders the children or redirects to the login screen.
 */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth();

  if (loading) {
    return <p style={{ fontFamily: 'system-ui, sans-serif', padding: '2rem' }}>Loading…</p>;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
