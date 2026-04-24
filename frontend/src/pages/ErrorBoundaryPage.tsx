import { isRouteErrorResponse, Link, useRouteError } from 'react-router-dom';
import { Button } from '@/shared/ui/button';

export function ErrorBoundaryPage() {
  const error = useRouteError();
  const heading = isRouteErrorResponse(error) ? `${error.status} ${error.statusText}` : 'Something went wrong';
  const detail =
    isRouteErrorResponse(error)
      ? error.data?.message ?? 'Unexpected route error.'
      : error instanceof Error
        ? error.message
        : 'An unexpected error occurred.';

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 px-6 text-center">
      <h1 className="text-3xl font-semibold tracking-tight">{heading}</h1>
      <p className="max-w-sm text-sm text-muted-foreground">{detail}</p>
      <Button asChild>
        <Link to="/tasks">Back to tasks</Link>
      </Button>
    </div>
  );
}
