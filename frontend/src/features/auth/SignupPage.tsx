import { type FormEvent, useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../../api/auth';
import type { ApiError } from '../../api/client';
import { AuthLayout, buttonStyle, errorStyle, fieldStyle } from '../../components/AuthLayout';

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
        <p style={{ marginBottom: 0 }}>
          <Link to="/login">Back to login</Link> · <Link to="/resend">Resend email</Link>
        </p>
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
        <button style={buttonStyle} type="submit" disabled={submitting}>
          {submitting ? 'Creating account…' : 'Sign up'}
        </button>
      </form>
      <p style={{ marginBottom: 0 }}>
        Already have an account? <Link to="/login">Log in</Link>
      </p>
    </AuthLayout>
  );
}
