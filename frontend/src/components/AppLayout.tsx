import { type CSSProperties } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { Button } from './Button';
import { colors } from './theme';

const navItems: [string, string][] = [
  ['/board', 'Board'],
  ['/teams', 'Teams'],
  ['/epics', 'Epics'],
  ['/tickets', 'Tickets'],
];

export function AppLayout() {
  const { user, logout } = useAuth();

  return (
    <div style={{ minHeight: '100vh', background: colors.pageBg, fontFamily: 'system-ui, sans-serif' }}>
      <header style={headerStyle}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1.5rem' }}>
          <span style={{ fontWeight: 700, color: colors.primary, fontSize: '1.05rem' }}>Ticketing</span>
          <nav style={{ display: 'flex', gap: '0.25rem' }}>
            {navItems.map(([to, label]) => (
              <NavLink
                key={to}
                to={to}
                style={({ isActive }) => ({ ...navLinkStyle, ...(isActive ? navLinkActiveStyle : {}) })}
              >
                {label}
              </NavLink>
            ))}
          </nav>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <span style={{ color: colors.muted, fontSize: '0.85rem' }}>{user?.email}</span>
          <Button variant="secondary" onClick={logout}>
            Log out
          </Button>
        </div>
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  );
}

const headerStyle: CSSProperties = {
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
  padding: '0.75rem 1.5rem',
  background: '#fff',
  borderBottom: `1px solid ${colors.border}`,
  flexWrap: 'wrap',
  gap: '0.75rem',
};

const navLinkStyle: CSSProperties = {
  padding: '0.4rem 0.75rem',
  borderRadius: 6,
  textDecoration: 'none',
  color: colors.text,
  fontSize: '0.9rem',
};

const navLinkActiveStyle: CSSProperties = {
  background: '#e7f0ff',
  color: colors.primary,
  fontWeight: 600,
};
