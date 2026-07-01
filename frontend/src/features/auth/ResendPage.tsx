import { type FormEvent, useState } from 'react';
import { authApi } from '../../api/auth';
import type { ApiError } from '../../api/client';
import { Button, LinkButton } from '../../components/Button';
import { AuthLayout, errorStyle, fieldStyle } from '../../components/AuthLayout';

export function ResendPage() {
  const [email, setEmail] = useState('');
  const [error, setError] = useState<ApiError | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [done, setDone] = useState(false);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await authApi.resend(email);
      setDone(true);
    } catch (err) {
      setError(err as ApiError);
    } finally {
      setSubmitting(false);
    }
  };

  if (done) {
    return (
      <AuthLayout title="Verification email sent">
        <p>If an unverified account exists for that email, a new verification link has been sent.</p>
        <LinkButton to="/login" variant="primary" style={{ width: '100%', textAlign: 'center' }}>
          Back to login
        </LinkButton>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Resend verification email">
      <form onSubmit={onSubmit}>
        <input
          style={fieldStyle}
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        {error && <p style={errorStyle}>{error.title}</p>}
        <Button type="submit" disabled={submitting} style={{ width: '100%' }}>
          {submitting ? 'Sending…' : 'Resend email'}
        </Button>
      </form>
      <LinkButton to="/login" variant="secondary" style={{ width: '100%', textAlign: 'center', marginTop: '0.75rem' }}>
        Back to login
      </LinkButton>
    </AuthLayout>
  );
}
