import { Calendar, CheckCircle2, Circle, MoreVertical, Pencil, RotateCcw, Trash2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';

import { Badge } from '@/shared/ui/badge';
import { Button } from '@/shared/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/shared/ui/dropdown-menu';
import { formatDate } from '@/shared/lib/date';
import { parseProblem } from '@/shared/lib/problemDetails';
import { cn } from '@/shared/lib/cn';
import type { TagDto } from '@/features/tags/types';

import {
  useCompleteTaskMutation,
  useDeleteTaskMutation,
  useReopenTaskMutation,
} from '../api';
import type { TaskDto } from '../types';
import { PriorityBadge } from './PriorityBadge';
import { StatusBadge } from './StatusBadge';

interface TaskCardProps {
  task: TaskDto;
  tags: readonly TagDto[];
  onEdit: (task: TaskDto) => void;
  onDelete: (task: TaskDto) => void;
  dragHandle?: React.ReactNode;
}

export function TaskCard({ task, tags, onEdit, onDelete, dragHandle }: TaskCardProps) {
  const [complete, { isLoading: completing }] = useCompleteTaskMutation();
  const [reopen, { isLoading: reopening }] = useReopenTaskMutation();
  const [, { isLoading: deleting }] = useDeleteTaskMutation({ fixedCacheKey: 'delete' });

  const isCompleted = task.status === 'Completed';
  const toggleLoading = completing || reopening;
  const taskTags = tags.filter((t) => task.tagIds.includes(t.id));

  async function toggleComplete() {
    try {
      if (isCompleted) {
        await reopen(task.id).unwrap();
      } else {
        await complete(task.id).unwrap();
      }
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(parsed.title, { description: parsed.detail });
    }
  }

  return (
    <article
      className={cn(
        'group relative flex flex-col gap-3 rounded-lg border bg-card p-4 shadow-sm transition-colors',
        isCompleted && 'opacity-80',
      )}
      aria-labelledby={`task-title-${task.id}`}
    >
      <div className="flex items-start gap-2">
        {dragHandle}
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="mt-0.5 h-6 w-6 shrink-0"
          disabled={toggleLoading || deleting}
          onClick={toggleComplete}
          aria-label={isCompleted ? `Reopen task ${task.title}` : `Mark ${task.title} as complete`}
        >
          {isCompleted ? (
            <CheckCircle2 className="h-5 w-5 text-emerald-500" />
          ) : (
            <Circle className="h-5 w-5" />
          )}
        </Button>
        <div className="min-w-0 flex-1">
          <Link
            to={`/tasks/${task.id}`}
            className="block text-sm font-medium leading-tight outline-none focus-visible:underline"
            id={`task-title-${task.id}`}
          >
            <span className={cn(isCompleted && 'line-through text-muted-foreground')}>
              {task.title}
            </span>
          </Link>
          {task.description && (
            <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{task.description}</p>
          )}
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-7 w-7" aria-label={`Open actions menu for ${task.title}`}>
              <MoreVertical className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => onEdit(task)}>
              <Pencil className="mr-2 h-4 w-4" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem onClick={toggleComplete} disabled={toggleLoading}>
              {isCompleted ? (
                <>
                  <RotateCcw className="mr-2 h-4 w-4" />
                  Reopen
                </>
              ) : (
                <>
                  <CheckCircle2 className="mr-2 h-4 w-4" />
                  Mark complete
                </>
              )}
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="text-destructive focus:text-destructive"
              onClick={() => onDelete(task)}
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <StatusBadge status={task.status} />
        <PriorityBadge priority={task.priority} />
        {task.dueDateUtc && (
          <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
            <Calendar className="h-3 w-3" aria-hidden />
            <span>{formatDate(task.dueDateUtc)}</span>
          </span>
        )}
        {taskTags.map((tag) => (
          <Badge key={tag.id} variant="outline" className="text-xs">
            #{tag.name}
          </Badge>
        ))}
      </div>
    </article>
  );
}
