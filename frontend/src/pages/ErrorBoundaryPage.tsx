import { isRouteErrorResponse, Link, useRouteError } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { Button } from '@/shared/ui/button';

export function ErrorBoundaryPage() {
  const { t } = useTranslation();
  const error = useRouteError();
  const heading = isRouteErrorResponse(error)
    ? `${error.status} ${error.statusText}`
    : t('errors.boundary.title');
  const detail = isRouteErrorResponse(error)
    ? (error.data?.message ?? t('errors.boundary.routeFallback'))
    : error instanceof Error
      ? error.message
      : t('errors.boundary.detail');

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 px-6 text-center">
      <h1 className="text-3xl font-semibold tracking-tight">{heading}</h1>
      <p className="max-w-sm text-sm text-muted-foreground">{detail}</p>
      <Button asChild>
        <Link to="/tasks">{t('common.backToTasks')}</Link>
      </Button>
    </div>
  );
}
