import { useState } from 'react';
import { Check, Flag } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { Popover, PopoverContent, PopoverTrigger } from '@/shared/ui/popover';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/shared/ui/tooltip';
import { cn } from '@/shared/lib/cn';
import { parseProblem, problemDetail, problemTitle } from '@/shared/lib/problemDetails';

import { useChangeTaskPriorityMutation } from '../api';
import { TASK_PRIORITIES, type TaskPriority } from '../types';
import { PriorityBadge } from './PriorityBadge';

const FLAG_COLOR: Record<TaskPriority, string> = {
  Low: 'text-(--color-priority-low)',
  Medium: 'text-(--color-priority-medium)',
  High: 'text-(--color-priority-high)',
  Critical: 'text-(--color-priority-critical)',
};

/**
 * Click-to-change priority control. Mirrors StatusPicker's interaction
 * shape: a popover menu with each option shown next to its coloured flag,
 * a check on the current selection, and error toasts on failure.
 */
export function PriorityPicker({ taskId, priority }: { taskId: string; priority: TaskPriority }) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [changePriority, { isLoading }] = useChangeTaskPriorityMutation();

  async function pick(next: TaskPriority) {
    setOpen(false);
    if (next === priority) return;
    try {
      await changePriority({ id: taskId, priority: next }).unwrap();
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
    }
  }

  const currentLabel = t(`tasks.priority.${priority}`);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <Tooltip>
        <TooltipTrigger asChild>
          <PopoverTrigger asChild>
            <button
              type="button"
              aria-haspopup="menu"
              aria-expanded={open}
              aria-label={t('tasks.priority.picker.trigger', { priority: currentLabel })}
              disabled={isLoading}
              onPointerDown={(e) => e.stopPropagation()}
              className="cursor-pointer rounded-md outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
            >
              <PriorityBadge priority={priority} />
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
        <div role="menu" aria-label={t('tasks.priority.picker.menu')} className="flex flex-col">
          {TASK_PRIORITIES.map((p) => {
            const isCurrent = p === priority;
            return (
              <button
                key={p}
                type="button"
                role="menuitemradio"
                aria-checked={isCurrent}
                onClick={() => void pick(p)}
                className="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent hover:text-accent-foreground focus-visible:bg-accent"
              >
                <Flag className={cn('h-4 w-4 shrink-0', FLAG_COLOR[p])} aria-hidden />
                <span className="flex-1 text-left">{t(`tasks.priority.${p}`)}</span>
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
