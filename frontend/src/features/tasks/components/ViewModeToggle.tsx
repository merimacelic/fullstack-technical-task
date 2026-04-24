import { LayoutGrid, List, Table2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { Tooltip, TooltipContent, TooltipTrigger } from '@/shared/ui/tooltip';
import { cn } from '@/shared/lib/cn';
import type { TaskViewMode } from '../types';

interface ViewModeToggleProps {
  value: TaskViewMode;
  onChange: (mode: TaskViewMode) => void;
}

const OPTIONS: readonly { value: TaskViewMode; key: string; icon: typeof List }[] = [
  { value: 'list', key: 'list', icon: List },
  { value: 'grid', key: 'grid', icon: LayoutGrid },
  { value: 'table', key: 'table', icon: Table2 },
];

export function ViewModeToggle({ value, onChange }: ViewModeToggleProps) {
  const { t } = useTranslation();
  return (
    <div
      role="radiogroup"
      aria-label={t('tasks.view.toggle')}
      className="inline-flex items-center rounded-md border bg-card p-0.5"
    >
      {OPTIONS.map(({ value: option, key, icon: Icon }) => {
        const selected = option === value;
        const label = t(`tasks.view.${key}`);
        return (
          <Tooltip key={option}>
            <TooltipTrigger asChild>
              <button
                type="button"
                role="radio"
                aria-checked={selected}
                aria-label={label}
                onClick={() => onChange(option)}
                className={cn(
                  'inline-flex h-7 w-8 items-center justify-center rounded-sm text-muted-foreground transition-colors',
                  'hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                  'cursor-pointer',
                  selected && 'bg-accent text-foreground shadow-sm',
                )}
              >
                <Icon className="h-4 w-4" aria-hidden />
              </button>
            </TooltipTrigger>
            <TooltipContent side="bottom">{label}</TooltipContent>
          </Tooltip>
        );
      })}
    </div>
  );
}
