import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type Announcements,
} from '@dnd-kit/core';
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';

import type { TagDto } from '@/features/tags/types';
import { useReorderTasks } from '../hooks/useReorderTasks';
import type { TaskDto, TaskFilters } from '../types';
import { SortableTaskCard } from './SortableTaskCard';

interface SortableTaskListProps {
  tasks: readonly TaskDto[];
  tags: readonly TagDto[];
  filters: TaskFilters;
  onEdit: (task: TaskDto) => void;
  onDelete: (task: TaskDto) => void;
}

// Accessible live-region announcements. dnd-kit defaults are fine; we override
// to make the task title read out so screen readers hear context.
function buildAnnouncements(tasks: readonly TaskDto[]): Announcements {
  const titleFor = (id: string | number) =>
    tasks.find((t) => t.id === String(id))?.title ?? String(id);

  return {
    onDragStart: ({ active }) => `Picked up task ${titleFor(active.id)}.`,
    onDragOver: ({ active, over }) =>
      over
        ? `Moving task ${titleFor(active.id)} over ${titleFor(over.id)}.`
        : `Task ${titleFor(active.id)} is no longer over a drop target.`,
    onDragEnd: ({ active, over }) =>
      over
        ? `Dropped task ${titleFor(active.id)} over ${titleFor(over.id)}.`
        : `Task ${titleFor(active.id)} was dropped back to its original position.`,
    onDragCancel: ({ active }) => `Movement cancelled for task ${titleFor(active.id)}.`,
  };
}

export function SortableTaskList({ tasks, tags, filters, onEdit, onDelete }: SortableTaskListProps) {
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );
  const { handleDragEnd } = useReorderTasks(tasks, filters);
  const ids = tasks.map((t) => t.id);

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragEnd={handleDragEnd}
      accessibility={{
        announcements: buildAnnouncements(tasks),
        screenReaderInstructions: {
          draggable:
            'Press space or enter to pick up a task. Use the arrow keys to move it. Press space or enter again to drop. Press escape to cancel.',
        },
      }}
    >
      <SortableContext items={ids} strategy={verticalListSortingStrategy}>
        <ul className="flex flex-col gap-3">
          {tasks.map((task) => (
            <SortableTaskCard
              key={task.id}
              task={task}
              tags={tags}
              onEdit={onEdit}
              onDelete={onDelete}
            />
          ))}
        </ul>
      </SortableContext>
    </DndContext>
  );
}
