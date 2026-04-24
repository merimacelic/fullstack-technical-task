import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { Button } from '@/shared/ui/button';

export function NotFoundPage() {
  const { t } = useTranslation();
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 px-6 text-center">
      <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">
        {t('errors.notFoundPage.code')}
      </p>
      <h1 className="text-3xl font-semibold tracking-tight">{t('errors.notFoundPage.title')}</h1>
      <p className="max-w-sm text-sm text-muted-foreground">{t('errors.notFoundPage.body')}</p>
      <Button asChild>
        <Link to="/tasks">{t('common.goToTasks')}</Link>
      </Button>
    </div>
  );
}
