import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { authApi } from '../../api/auth';
import { AuthLayout } from '../../components/AuthLayout';

type Status = 'verifying' | 'success' | 'error';

export function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const [status, setStatus] = useState<Status>('verifying');
  const token = searchParams.get('token');

  useEffect(() => {
    if (!token) {
      setStatus('error');
      return;
    }

    authApi
      .verifyEmail(token)
      .then(() => setStatus('success'))
      .catch(() => setStatus('error'));
  }, [token]);

  return (
    <AuthLayout title="Email verification">
      {status === 'verifying' && <p>Verifying your email…</p>}
      {status === 'success' && (
        <>
          <p>Your email has been verified. You can now log in.</p>
          <p style={{ marginBottom: 0 }}>
            <Link to="/login">Go to login</Link>
          </p>
        </>
      )}
      {status === 'error' && (
        <>
          <p>This verification link is invalid or has expired.</p>
          <p style={{ marginBottom: 0 }}>
            <Link to="/resend">Request a new link</Link>
          </p>
        </>
      )}
    </AuthLayout>
  );
}
