import { GripVertical } from 'lucide-react';
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
}

export function SortableTaskCard({ task, tags, onEdit, onDelete }: SortableTaskCardProps) {
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
      className={cn(isDragging && 'opacity-60 ring-2 ring-ring')}
    >
      <TaskCard
        task={task}
        tags={tags}
        onEdit={onEdit}
        onDelete={onDelete}
        dragHandle={
          <button
            type="button"
            {...attributes}
            {...listeners}
            className="cursor-grab touch-none rounded-md p-1 text-muted-foreground outline-none hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring active:cursor-grabbing"
            aria-label={`Drag to reorder ${task.title}`}
          >
            <GripVertical className="h-4 w-4" aria-hidden />
          </button>
        }
      />
    </li>
  );
}
