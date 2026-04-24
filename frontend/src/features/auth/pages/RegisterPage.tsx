import { useTranslation } from 'react-i18next';

import { RegisterForm } from '../components/RegisterForm';
import { AuthShell } from './AuthShell';

export function RegisterPage() {
  const { t } = useTranslation();
  return (
    <AuthShell title={t('auth.register.title')} description={t('auth.register.description')}>
      <RegisterForm />
    </AuthShell>
  );
}
