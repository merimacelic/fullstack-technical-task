import type { DraggableAttributes } from '@dnd-kit/core';
import type { SyntheticListenerMap } from '@dnd-kit/core/dist/hooks/utilities';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { ArrowUpRight, Pencil, Trash2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { Button } from '@/shared/ui/button';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/shared/ui/tooltip';
import { formatDate } from '@/shared/lib/date';
import { cn } from '@/shared/lib/cn';
import type { TagDto } from '@/features/tags/types';

import type { TaskDto } from '../types';
import { PriorityPicker } from './PriorityPicker';
import { StatusPicker } from './StatusPicker';
import { TagOverflow } from './TagOverflow';

interface TaskTableRowProps {
  task: TaskDto;
  tags: readonly TagDto[];
  onEdit: (task: TaskDto) => void;
  onDelete: (task: TaskDto) => void;
  // Drag wiring (supplied only by the sortable wrapper).
  rowRef?: (node: HTMLTableRowElement | null) => void;
  style?: React.CSSProperties;
  dragAttributes?: DraggableAttributes;
  dragListeners?: SyntheticListenerMap;
  isDragging?: boolean;
}

export function TaskTableRow({
  task,
  tags,
  onEdit,
  onDelete,
  rowRef,
  style,
  dragAttributes,
  dragListeners,
  isDragging,
}: TaskTableRowProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const isCompleted = task.status === 'Completed';
  const taskTags = tags.filter((tag) => task.tagIds.includes(tag.id));
  const draggable = Boolean(dragListeners);

  return (
    <tr
      ref={rowRef}
      style={style}
      className={cn(
        'group border-b transition-colors hover:bg-accent/40',
        draggable && 'cursor-grab touch-none',
        isDragging && 'cursor-grabbing opacity-60',
        isCompleted && 'opacity-80',
      )}
      {...dragAttributes}
      {...dragListeners}
    >
      <td className="w-10 py-3 pl-3 pr-2 align-top">
        <StatusPicker taskId={task.id} status={task.status} variant="icon" />
      </td>
      <td className="min-w-[220px] py-3 pr-3 align-top">
        <p
          className={cn(
            'text-sm font-medium leading-tight',
            isCompleted && 'line-through text-muted-foreground',
          )}
        >
          {task.title}
        </p>
        <p className="mt-0.5 line-clamp-1 text-xs text-muted-foreground">
          {task.description || ' '}
        </p>
      </td>
      <td className="py-3 pr-3 align-top">
        <StatusPicker taskId={task.id} status={task.status} variant="pill" />
      </td>
      <td className="py-3 pr-3 align-top">
        <PriorityPicker taskId={task.id} priority={task.priority} />
      </td>
      <td className="whitespace-nowrap py-3 pr-3 align-top text-xs text-muted-foreground">
        {task.dueDateUtc ? formatDate(task.dueDateUtc) : '—'}
      </td>
      <td className="py-3 pr-3 align-top">
        {taskTags.length === 0 ? (
          <span className="text-xs text-muted-foreground">—</span>
        ) : (
          <div className="flex flex-nowrap items-center gap-1 overflow-hidden">
            <TagOverflow tags={taskTags} max={3} />
          </div>
        )}
      </td>
      <td className="py-3 pr-3 align-top">
        <div className="flex items-center justify-start gap-1">
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-7 w-7 cursor-pointer"
                onClick={() => onEdit(task)}
                aria-label={t('tasks.card.aria.edit', { title: task.title })}
              >
                <Pencil className="h-4 w-4" />
              </Button>
            </TooltipTrigger>
            <TooltipContent side="top">{t('tasks.card.tooltip.edit')}</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-7 w-7 cursor-pointer"
                onClick={() => navigate(`/tasks/${task.id}`)}
                aria-label={t('tasks.card.aria.details', { title: task.title })}
              >
                <ArrowUpRight className="h-4 w-4" />
              </Button>
            </TooltipTrigger>
            <TooltipContent side="top">{t('tasks.card.tooltip.details')}</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-7 w-7 cursor-pointer text-destructive hover:text-destructive"
                onClick={() => onDelete(task)}
                aria-label={t('tasks.card.aria.delete', { title: task.title })}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </TooltipTrigger>
            <TooltipContent side="top">{t('tasks.card.tooltip.delete')}</TooltipContent>
          </Tooltip>
        </div>
      </td>
    </tr>
  );
}

interface SortableTaskTableRowProps {
  task: TaskDto;
  tags: readonly TagDto[];
  onEdit: (task: TaskDto) => void;
  onDelete: (task: TaskDto) => void;
}

export function SortableTaskTableRow({ task, tags, onEdit, onDelete }: SortableTaskTableRowProps) {
  const { setNodeRef, attributes, listeners, transform, transition, isDragging } = useSortable({
    id: task.id,
  });

  return (
    <TaskTableRow
      task={task}
      tags={tags}
      onEdit={onEdit}
      onDelete={onDelete}
      rowRef={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      dragAttributes={attributes}
      dragListeners={listeners}
      isDragging={isDragging}
    />
  );
}
