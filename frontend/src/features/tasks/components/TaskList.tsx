import { Skeleton } from '@/shared/ui/skeleton';
import type { TagDto } from '@/features/tags/types';
import type { TaskDto } from '../types';
import { TaskCard } from './TaskCard';

interface TaskListProps {
  tasks: readonly TaskDto[];
  tags: readonly TagDto[];
  isLoading: boolean;
  onEdit: (task: TaskDto) => void;
  onDelete: (task: TaskDto) => void;
}

export function TaskList({ tasks, tags, isLoading, onEdit, onDelete }: TaskListProps) {
  if (isLoading && tasks.length === 0) {
    return (
      <ul className="flex flex-col gap-3" aria-busy="true" aria-label="Loading tasks">
        {Array.from({ length: 4 }).map((_, i) => (
          <li key={i}>
            <Skeleton className="h-[92px] w-full" />
          </li>
        ))}
      </ul>
    );
  }

  if (tasks.length === 0) {
    return (
      <div className="rounded-lg border border-dashed bg-card p-10 text-center">
        <p className="text-sm font-medium">No tasks match your filters.</p>
        <p className="mt-1 text-xs text-muted-foreground">
          Create a new task or clear the filters to see more.
        </p>
      </div>
    );
  }

  return (
    <ul className="flex flex-col gap-3">
      {tasks.map((task) => (
        <li key={task.id}>
          <TaskCard task={task} tags={tags} onEdit={onEdit} onDelete={onDelete} />
        </li>
      ))}
    </ul>
  );
}
