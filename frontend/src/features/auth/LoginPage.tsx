import { type FormEvent, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import type { ApiError } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { Button, LinkButton } from '../../components/Button';
import { AuthLayout, errorStyle, fieldStyle } from '../../components/AuthLayout';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<ApiError | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(email, password);
      navigate('/');
    } catch (err) {
      setError(err as ApiError);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <AuthLayout title="Log in">
      <form onSubmit={onSubmit}>
        <input
          style={fieldStyle}
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <input
          style={fieldStyle}
          type="password"
          placeholder="Password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        {error && (
          <p style={errorStyle}>
            {error.title}
            {error.status === 403 && (
              <>
                {' — '}
                <Link to="/resend">resend verification email</Link>
              </>
            )}
          </p>
        )}
        <Button type="submit" disabled={submitting} style={{ width: '100%' }}>
          {submitting ? 'Logging in…' : 'Log in'}
        </Button>
      </form>
      <LinkButton to="/signup" variant="secondary" style={{ width: '100%', textAlign: 'center', marginTop: '0.75rem' }}>
        Create an account
      </LinkButton>
    </AuthLayout>
  );
}
