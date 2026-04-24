import { useTranslation } from 'react-i18next';

import { cn } from '@/shared/lib/cn';
import type { TaskStatus } from '../types';

const STYLES: Record<TaskStatus, string> = {
  Pending: 'bg-muted text-muted-foreground border-border',
  InProgress: 'bg-blue-500/15 text-blue-600 border-blue-500/30 dark:text-blue-400',
  Completed: 'bg-emerald-500/15 text-emerald-600 border-emerald-500/30 dark:text-emerald-400',
};

export function StatusBadge({ status, className }: { status: TaskStatus; className?: string }) {
  const { t } = useTranslation();
  const label = t(`tasks.status.${status}`);
  return (
    <span
      className={cn(
        'inline-flex shrink-0 items-center rounded-md border px-2 py-0.5 text-xs font-medium',
        STYLES[status],
        className,
      )}
      aria-label={t('tasks.status.ariaLabel', { label })}
    >
      {label}
    </span>
  );
}
