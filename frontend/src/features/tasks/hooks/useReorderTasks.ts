import type { DragEndEvent } from '@dnd-kit/core';

import { useReorderTaskMutation } from '../api';
import { parseProblem } from '@/shared/lib/problemDetails';
import type { TaskDto, TaskFilters } from '../types';
import { toast } from 'sonner';

export function useReorderTasks(tasks: readonly TaskDto[], filters: TaskFilters) {
  const [reorder] = useReorderTaskMutation();

  async function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const activeId = String(active.id);
    const overId = String(over.id);
    const activeIndex = tasks.findIndex((t) => t.id === activeId);
    const overIndex = tasks.findIndex((t) => t.id === overId);
    if (activeIndex < 0 || overIndex < 0) return;

    // Compute neighbours for the server: the IDs that should sit immediately
    // above / below the moved task *after* the move.
    const next = [...tasks];
    const moved = next.splice(activeIndex, 1)[0];
    if (!moved) return;
    next.splice(overIndex, 0, moved);

    const finalIdx = next.findIndex((t) => t.id === activeId);
    const prev = finalIdx > 0 ? next[finalIdx - 1] : undefined;
    const after = finalIdx < next.length - 1 ? next[finalIdx + 1] : undefined;
    const previousTaskId = prev ? prev.id : null;
    const nextTaskId = after ? after.id : null;

    try {
      await reorder({
        id: activeId,
        previousTaskId,
        nextTaskId,
        filters,
      }).unwrap();
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(parsed.title, { description: parsed.detail ?? 'Could not reorder task.' });
    }
  }

  return { handleDragEnd };
}
