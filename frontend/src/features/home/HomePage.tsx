import { Link } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { buttonStyle } from '../../components/AuthLayout';

/**
 * Placeholder for the authenticated area. The Kanban board replaces this in a later phase.
 */
export function HomePage() {
  const { user, logout } = useAuth();

  return (
    <main style={{ fontFamily: 'system-ui, sans-serif', padding: '2rem', maxWidth: 640, margin: '0 auto' }}>
      <h1>Ticketing System</h1>
      <p>
        Signed in as <strong>{user?.email}</strong>.
      </p>
      <p>
        Manage <Link to="/teams">Teams</Link>. The Kanban board and other screens will appear here in
        later phases.
      </p>
      <button style={{ ...buttonStyle, width: 'auto', padding: '0.5rem 1rem' }} onClick={logout}>
        Log out
      </button>
    </main>
  );
}
