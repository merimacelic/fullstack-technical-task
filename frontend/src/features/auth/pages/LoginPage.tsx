import { useTranslation } from 'react-i18next';

import { LoginForm } from '../components/LoginForm';
import { AuthShell } from './AuthShell';

export function LoginPage() {
  const { t } = useTranslation();
  return (
    <AuthShell title={t('auth.login.title')} description={t('auth.login.description')}>
      <LoginForm />
    </AuthShell>
  );
}
