import { type FormEvent, useState } from 'react';
import { authApi } from '../../api/auth';
import type { ApiError } from '../../api/client';
import { Button, LinkButton } from '../../components/Button';
import { AuthLayout, errorStyle, fieldStyle } from '../../components/AuthLayout';

export function SignupPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<ApiError | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [done, setDone] = useState(false);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await authApi.signup(email, password);
      setDone(true);
    } catch (err) {
      setError(err as ApiError);
    } finally {
      setSubmitting(false);
    }
  };

  if (done) {
    return (
      <AuthLayout title="Check your email">
        <p>
          We sent a verification link to <strong>{email}</strong>. Open it to activate your account,
          then log in.
        </p>
        <div style={{ display: 'grid', gap: '0.5rem' }}>
          <LinkButton to="/login" variant="primary" style={{ textAlign: 'center' }}>
            Back to login
          </LinkButton>
          <LinkButton to="/resend" variant="secondary" style={{ textAlign: 'center' }}>
            Resend email
          </LinkButton>
        </div>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Sign up">
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
          placeholder="Password (min. 8 characters)"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          minLength={8}
          required
        />
        {error && <p style={errorStyle}>{error.title}</p>}
        <Button type="submit" disabled={submitting} style={{ width: '100%' }}>
          {submitting ? 'Creating account…' : 'Sign up'}
        </Button>
      </form>
      <LinkButton to="/login" variant="secondary" style={{ width: '100%', textAlign: 'center', marginTop: '0.75rem' }}>
        Back to login
      </LinkButton>
    </AuthLayout>
  );
}
