import { useState, type ComponentType } from 'react';
import { Check, CheckCircle2, Circle, CircleDashed } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { Popover, PopoverContent, PopoverTrigger } from '@/shared/ui/popover';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/shared/ui/tooltip';
import { cn } from '@/shared/lib/cn';
import { parseProblem, problemDetail, problemTitle } from '@/shared/lib/problemDetails';

import { useChangeTaskStatusMutation } from '../api';
import { TASK_STATUSES, type TaskStatus } from '../types';
import { StatusBadge } from './StatusBadge';

// Lucide lacks a true "half-filled" circle (Linear's signature in-progress
// glyph), so we use CircleDashed + a distinct color to separate it from the
// empty Pending circle. Shape *and* color signal state — WCAG 1.4.1.
const ICONS: Record<TaskStatus, ComponentType<{ className?: string; 'aria-hidden'?: boolean }>> = {
  Pending: Circle,
  InProgress: CircleDashed,
  Completed: CheckCircle2,
};

const ICON_COLOR: Record<TaskStatus, string> = {
  Pending: 'text-muted-foreground',
  InProgress: 'text-blue-500',
  Completed: 'text-emerald-500',
};

interface StatusPickerProps {
  taskId: string;
  status: TaskStatus;
  /**
   * `icon` — single-glyph trigger (Linear-style, used on cards and tables).
   * `pill` — full StatusBadge as the trigger (used where the label matters).
   */
  variant?: 'icon' | 'pill';
  /** Optional id — useful when labelling the trigger from another element. */
  id?: string;
}

/**
 * Click-to-change status control. Opens a popover menu with the three
 * statuses and a checkmark on the current one. Replaces the old binary
 * complete-toggle; menu-first interaction mirrors Linear/Asana conventions.
 */
export function StatusPicker({ taskId, status, variant = 'icon', id }: StatusPickerProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [changeStatus, { isLoading }] = useChangeTaskStatusMutation();

  async function pick(next: TaskStatus) {
    setOpen(false);
    if (next === status) return;
    try {
      await changeStatus({ id: taskId, status: next }).unwrap();
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
    }
  }

  const currentLabel = t(`tasks.status.${status}`);
  const Icon = ICONS[status];

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <Tooltip>
        <TooltipTrigger asChild>
          <PopoverTrigger asChild>
            <button
              id={id}
              type="button"
              aria-haspopup="menu"
              aria-expanded={open}
              aria-label={t('tasks.status.picker.trigger', { status: currentLabel })}
              disabled={isLoading}
              // The TaskCard body listens for pointer-down to start a drag;
              // stop it so clicking the trigger opens the menu instead.
              onPointerDown={(e) => e.stopPropagation()}
              className={cn(
                'cursor-pointer rounded-md outline-none disabled:opacity-50',
                'focus-visible:ring-2 focus-visible:ring-ring',
                variant === 'icon' &&
                  'inline-flex h-6 w-6 shrink-0 items-center justify-center hover:bg-accent',
              )}
            >
              {variant === 'icon' ? (
                <Icon className={cn('h-5 w-5', ICON_COLOR[status])} aria-hidden />
              ) : (
                <StatusBadge status={status} />
              )}
            </button>
          </PopoverTrigger>
        </TooltipTrigger>
        {!open && <TooltipContent side="top">{currentLabel}</TooltipContent>}
      </Tooltip>
      <PopoverContent
        align="start"
        className="w-[180px] p-1"
        onPointerDown={(e) => e.stopPropagation()}
      >
        <div role="menu" aria-label={t('tasks.status.picker.menu')} className="flex flex-col">
          {TASK_STATUSES.map((s) => {
            const isCurrent = s === status;
            const OptionIcon = ICONS[s];
            return (
              <button
                key={s}
                type="button"
                role="menuitemradio"
                aria-checked={isCurrent}
                onClick={() => void pick(s)}
                className="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent hover:text-accent-foreground focus-visible:bg-accent"
              >
                <OptionIcon className={cn('h-4 w-4 shrink-0', ICON_COLOR[s])} aria-hidden />
                <span className="flex-1 text-left">{t(`tasks.status.${s}`)}</span>
                <Check
                  className={cn('h-4 w-4 shrink-0', isCurrent ? 'opacity-100' : 'opacity-0')}
                  aria-hidden
                />
              </button>
            );
          })}
        </div>
      </PopoverContent>
    </Popover>
  );
}
