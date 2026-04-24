import type { DragEndEvent } from '@dnd-kit/core';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { useLazyGetTasksQuery, useReorderTaskMutation } from '../api';
import { parseProblem, problemDetail, problemTitle } from '@/shared/lib/problemDetails';
import type { TaskDto, TaskFilters } from '../types';

interface ReorderBounds {
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
}

export function useReorderTasks(
  tasks: readonly TaskDto[],
  filters: TaskFilters,
  bounds: ReorderBounds = {},
) {
  const { t } = useTranslation();
  const [reorder] = useReorderTaskMutation();
  const [fetchTasksPage] = useLazyGetTasksQuery();

  async function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const activeId = String(active.id);
    const overId = String(over.id);
    const activeIndex = tasks.findIndex((task) => task.id === activeId);
    const overIndex = tasks.findIndex((task) => task.id === overId);
    if (activeIndex < 0 || overIndex < 0) return;

    const next = [...tasks];
    const moved = next.splice(activeIndex, 1)[0];
    if (!moved) return;
    next.splice(overIndex, 0, moved);

    const finalIdx = next.findIndex((task) => task.id === activeId);
    const prev = finalIdx > 0 ? next[finalIdx - 1] : undefined;
    const after = finalIdx < next.length - 1 ? next[finalIdx + 1] : undefined;
    let visualPreviousId: string | null = prev ? prev.id : null;
    let visualNextId: string | null = after ? after.id : null;

    try {
      if (visualPreviousId === null && bounds.hasPreviousPage) {
        const prevPage = await fetchTasksPage(
          { ...filters, page: filters.page - 1 },
          true,
        ).unwrap();
        const lastOnPrevPage = prevPage.items.at(-1);
        if (!lastOnPrevPage) return;
        visualPreviousId = lastOnPrevPage.id;
      }
      if (visualNextId === null && bounds.hasNextPage) {
        const nextPage = await fetchTasksPage(
          { ...filters, page: filters.page + 1 },
          true,
        ).unwrap();
        const firstOnNextPage = nextPage.items[0];
        if (!firstOnNextPage) return;
        visualNextId = firstOnNextPage.id;
      }
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
      return;
    }

    try {
      await reorder({
        id: activeId,
        visualPreviousId,
        visualNextId,
        filters,
      }).unwrap();
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
    }
  }

  return { handleDragEnd };
}
