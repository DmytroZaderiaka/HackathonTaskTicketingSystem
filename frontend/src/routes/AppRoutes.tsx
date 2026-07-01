import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../features/auth/LoginPage';
import { SignupPage } from '../features/auth/SignupPage';
import { VerifyEmailPage } from '../features/auth/VerifyEmailPage';
import { ResendPage } from '../features/auth/ResendPage';
import { HomePage } from '../features/home/HomePage';
import { TeamsPage } from '../features/teams/TeamsPage';
import { EpicsPage } from '../features/epics/EpicsPage';
import { RequireAuth } from './RequireAuth';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/signup" element={<SignupPage />} />
      <Route path="/verify-email" element={<VerifyEmailPage />} />
      <Route path="/resend" element={<ResendPage />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <HomePage />
          </RequireAuth>
        }
      />
      <Route
        path="/teams"
        element={
          <RequireAuth>
            <TeamsPage />
          </RequireAuth>
        }
      />
      <Route
        path="/epics"
        element={
          <RequireAuth>
            <EpicsPage />
          </RequireAuth>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
