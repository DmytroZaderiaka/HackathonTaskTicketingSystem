import { type FormEvent, useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../../api/auth';
import type { ApiError } from '../../api/client';
import { AuthLayout, buttonStyle, errorStyle, fieldStyle } from '../../components/AuthLayout';

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
        <p style={{ marginBottom: 0 }}>
          <Link to="/login">Back to login</Link>
        </p>
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
        <button style={buttonStyle} type="submit" disabled={submitting}>
          {submitting ? 'Sending…' : 'Resend email'}
        </button>
      </form>
      <p style={{ marginBottom: 0 }}>
        <Link to="/login">Back to login</Link>
      </p>
    </AuthLayout>
  );
}
