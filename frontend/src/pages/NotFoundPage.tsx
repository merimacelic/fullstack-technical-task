import { Link } from 'react-router-dom';
import { Button } from '@/shared/ui/button';

export function NotFoundPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 px-6 text-center">
      <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">404</p>
      <h1 className="text-3xl font-semibold tracking-tight">Page not found</h1>
      <p className="max-w-sm text-sm text-muted-foreground">
        The page you tried to reach doesn&apos;t exist. Head back to your tasks to continue.
      </p>
      <Button asChild>
        <Link to="/tasks">Go to tasks</Link>
      </Button>
    </div>
  );
}
