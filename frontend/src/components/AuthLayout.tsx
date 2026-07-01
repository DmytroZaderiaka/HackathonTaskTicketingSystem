import type { CSSProperties, ReactNode } from 'react';

export function AuthLayout({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div style={pageStyle}>
      <div style={cardStyle}>
        <h1 style={{ fontSize: '1.4rem', marginTop: 0 }}>{title}</h1>
        {children}
      </div>
    </div>
  );
}

const pageStyle: CSSProperties = {
  minHeight: '100vh',
  display: 'grid',
  placeItems: 'center',
  fontFamily: 'system-ui, sans-serif',
  background: '#f4f5f7',
  padding: '1rem',
};

const cardStyle: CSSProperties = {
  width: 360,
  maxWidth: '100%',
  background: '#fff',
  padding: '2rem',
  borderRadius: 8,
  boxShadow: '0 1px 4px rgba(0,0,0,0.12)',
};

export const fieldStyle: CSSProperties = {
  display: 'block',
  width: '100%',
  boxSizing: 'border-box',
  padding: '0.5rem',
  marginBottom: '0.75rem',
  border: '1px solid #ccc',
  borderRadius: 4,
};

export const errorStyle: CSSProperties = {
  color: '#b00020',
  margin: '0 0 0.75rem',
};
