import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';

import { cn } from '@/shared/lib/cn';
import type { TagDto } from '@/features/tags/types';
import type { TaskDto } from '../types';
import { TaskCard } from './TaskCard';

interface SortableTaskCardProps {
  task: TaskDto;
  tags: readonly TagDto[];
  onEdit: (task: TaskDto) => void;
  onDelete: (task: TaskDto) => void;
  fullHeight?: boolean;
}

export function SortableTaskCard({
  task,
  tags,
  onEdit,
  onDelete,
  fullHeight,
}: SortableTaskCardProps) {
  const { setNodeRef, attributes, listeners, transform, transition, isDragging } = useSortable({
    id: task.id,
  });

  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <li
      ref={setNodeRef}
      style={style}
      className={cn(fullHeight && 'h-full', isDragging && 'opacity-60 ring-2 ring-ring')}
    >
      <TaskCard
        task={task}
        tags={tags}
        onEdit={onEdit}
        onDelete={onDelete}
        dragAttributes={attributes}
        dragListeners={listeners}
        isDragging={isDragging}
      />
    </li>
  );
}
