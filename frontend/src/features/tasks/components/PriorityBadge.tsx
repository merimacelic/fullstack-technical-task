import { Flag } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { cn } from '@/shared/lib/cn';
import type { TaskPriority } from '../types';

const STYLES: Record<TaskPriority, string> = {
  Low: 'bg-(--color-priority-low)/15 text-(--color-priority-low) border-(--color-priority-low)/30',
  Medium: 'bg-(--color-priority-medium)/15 text-(--color-priority-medium) border-(--color-priority-medium)/30',
  High: 'bg-(--color-priority-high)/15 text-(--color-priority-high) border-(--color-priority-high)/30',
  Critical: 'bg-(--color-priority-critical)/15 text-(--color-priority-critical) border-(--color-priority-critical)/40',
};

export function PriorityBadge({
  priority,
  className,
}: {
  priority: TaskPriority;
  className?: string;
}) {
  const { t } = useTranslation();
  const label = t(`tasks.priority.${priority}`);
  return (
    <span
      className={cn(
        'inline-flex shrink-0 items-center gap-1 rounded-md border px-2 py-0.5 text-xs font-medium',
        STYLES[priority],
        className,
      )}
      aria-label={t('tasks.priority.ariaLabel', { label })}
    >
      <Flag className="h-3 w-3" aria-hidden />
      {label}
    </span>
  );
}
