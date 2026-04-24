import { RegisterForm } from '../components/RegisterForm';
import { AuthShell } from './AuthShell';

export function RegisterPage() {
  return (
    <AuthShell
      title="Create your account"
      description="Start organising your tasks in seconds."
    >
      <RegisterForm />
    </AuthShell>
  );
}
