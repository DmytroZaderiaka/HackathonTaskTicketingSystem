import { useEffect, useState } from 'react';

type HealthState = 'loading' | 'healthy' | 'error';

/**
 * Phase 0 stub screen. It only verifies that the SPA is served and can reach the
 * backend through the `/api` reverse proxy. Real screens arrive in later phases.
 */
export function App() {
  const [health, setHealth] = useState<HealthState>('loading');

  useEffect(() => {
    fetch('/api/health')
      .then((response) => setHealth(response.ok ? 'healthy' : 'error'))
      .catch(() => setHealth('error'));
  }, []);

  return (
    <main style={{ fontFamily: 'system-ui, sans-serif', padding: '2rem' }}>
      <h1>Ticketing System</h1>
      <p>Scaffold is running.</p>
      <p>
        Backend API:{' '}
        {health === 'loading' && <span>checking…</span>}
        {health === 'healthy' && <span>reachable</span>}
        {health === 'error' && <span>unreachable</span>}
      </p>
    </main>
  );
}
