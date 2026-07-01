import type { ButtonHTMLAttributes, CSSProperties } from 'react';
import { Link, type LinkProps } from 'react-router-dom';
import { colors } from './theme';

type Variant = 'primary' | 'secondary' | 'danger';

const base: CSSProperties = {
  padding: '0.5rem 1rem',
  borderRadius: 6,
  cursor: 'pointer',
  fontSize: '0.9rem',
  fontWeight: 500,
  border: '1px solid transparent',
  textDecoration: 'none',
  display: 'inline-block',
  lineHeight: 1.2,
};

const variants: Record<Variant, CSSProperties> = {
  primary: { background: colors.primary, color: '#fff' },
  secondary: { background: '#fff', color: colors.text, borderColor: colors.border },
  danger: { background: '#fff', color: colors.danger, borderColor: '#f0b4b4' },
};

export function Button({
  variant = 'primary',
  style,
  disabled,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variant }) {
  return (
    <button
      disabled={disabled}
      style={{ ...base, ...variants[variant], opacity: disabled ? 0.6 : 1, ...style }}
      {...props}
    />
  );
}

export function LinkButton({ variant = 'secondary', style, ...props }: LinkProps & { variant?: Variant }) {
  return <Link style={{ ...base, ...variants[variant], ...style }} {...props} />;
}
