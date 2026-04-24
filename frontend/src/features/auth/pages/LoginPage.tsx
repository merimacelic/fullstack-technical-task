import { LoginForm } from '../components/LoginForm';
import { AuthShell } from './AuthShell';

export function LoginPage() {
  return (
    <AuthShell
      title="Welcome back"
      description="Sign in to manage your tasks."
    >
      <LoginForm />
    </AuthShell>
  );
}
