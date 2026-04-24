import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type Announcements,
  type Modifier,
} from '@dnd-kit/core';
import {
  restrictToFirstScrollableAncestor,
  restrictToParentElement,
  restrictToVerticalAxis,
} from '@dnd-kit/modifiers';
import {
  SortableContext,
  rectSortingStrategy,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { useTranslation } from 'react-i18next';
import type { TFunction } from 'i18next';

import { Skeleton } from '@/shared/ui/skeleton';
import type { TagDto } from '@/features/tags/types';

import { useReorderTasks } from '../hooks/useReorderTasks';
import type { TaskDto, TaskFilters, TaskViewMode } from '../types';
import { SortableTaskCard } from './SortableTaskCard';
import { TaskCard } from './TaskCard';
import { SortableTaskTableRow, TaskTableRow } from './TaskTableRow';

interface TaskViewProps {
  tasks: readonly TaskDto[];
  tags: readonly TagDto[];
  viewMode: TaskViewMode;
  draggable: boolean;
  filters: TaskFilters;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  isLoading: boolean;
  onEdit: (task: TaskDto) => void;
  onDelete: (task: TaskDto) => void;
}

export function TaskView({
  tasks,
  tags,
  viewMode,
  draggable,
  filters,
  hasPreviousPage,
  hasNextPage,
  isLoading,
  onEdit,
  onDelete,
}: TaskViewProps) {
  if (isLoading && tasks.length === 0) {
    return <LoadingState viewMode={viewMode} />;
  }

  if (tasks.length === 0) {
    return <EmptyState />;
  }

  if (!draggable) {
    return (
      <StaticView
        tasks={tasks}
        tags={tags}
        viewMode={viewMode}
        onEdit={onEdit}
        onDelete={onDelete}
      />
    );
  }

  return (
    <SortableView
      tasks={tasks}
      tags={tags}
      viewMode={viewMode}
      filters={filters}
      hasPreviousPage={hasPreviousPage}
      hasNextPage={hasNextPage}
      onEdit={onEdit}
      onDelete={onDelete}
    />
  );
}

function StaticView({
  tasks,
  tags,
  viewMode,
  onEdit,
  onDelete,
}: Pick<TaskViewProps, 'tasks' | 'tags' | 'viewMode' | 'onEdit' | 'onDelete'>) {
  if (viewMode === 'table') {
    return (
      <TableShell>
        {tasks.map((task) => (
          <TaskTableRow key={task.id} task={task} tags={tags} onEdit={onEdit} onDelete={onDelete} />
        ))}
      </TableShell>
    );
  }

  const listClass =
    viewMode === 'grid'
      ? 'grid gap-3 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3'
      : 'flex flex-col gap-3';

  return (
    <ul className={listClass}>
      {tasks.map((task) => (
        <li key={task.id} className={viewMode === 'grid' ? 'h-full' : ''}>
          <TaskCard task={task} tags={tags} onEdit={onEdit} onDelete={onDelete} />
        </li>
      ))}
    </ul>
  );
}

function SortableView({
  tasks,
  tags,
  viewMode,
  filters,
  hasPreviousPage,
  hasNextPage,
  onEdit,
  onDelete,
}: Pick<
  TaskViewProps,
  | 'tasks'
  | 'tags'
  | 'viewMode'
  | 'filters'
  | 'hasPreviousPage'
  | 'hasNextPage'
  | 'onEdit'
  | 'onDelete'
>) {
  const { t } = useTranslation();
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );
  const { handleDragEnd } = useReorderTasks(tasks, filters, { hasPreviousPage, hasNextPage });
  const ids = tasks.map((task) => task.id);
  const strategy = viewMode === 'grid' ? rectSortingStrategy : verticalListSortingStrategy;

  const modifiers: Modifier[] =
    viewMode === 'grid'
      ? [restrictToParentElement, restrictToFirstScrollableAncestor]
      : [restrictToVerticalAxis, restrictToParentElement, restrictToFirstScrollableAncestor];

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      modifiers={modifiers}
      onDragEnd={handleDragEnd}
      accessibility={{
        announcements: buildAnnouncements(tasks, t),
        screenReaderInstructions: {
          draggable: t('tasks.dnd.instructions'),
        },
      }}
    >
      <SortableContext items={ids} strategy={strategy}>
        {viewMode === 'table' ? (
          <TableShell>
            {tasks.map((task) => (
              <SortableTaskTableRow
                key={task.id}
                task={task}
                tags={tags}
                onEdit={onEdit}
                onDelete={onDelete}
              />
            ))}
          </TableShell>
        ) : (
          <ul
            className={
              viewMode === 'grid'
                ? 'grid gap-3 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3'
                : 'flex flex-col gap-3'
            }
          >
            {tasks.map((task) => (
              <SortableTaskCard
                key={task.id}
                task={task}
                tags={tags}
                onEdit={onEdit}
                onDelete={onDelete}
                fullHeight={viewMode === 'grid'}
              />
            ))}
          </ul>
        )}
      </SortableContext>
    </DndContext>
  );
}

function TableShell({ children }: { children: React.ReactNode }) {
  const { t } = useTranslation();
  return (
    <div className="overflow-x-auto rounded-lg border bg-card">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-xs uppercase tracking-wide text-muted-foreground">
          <tr>
            <th className="w-10 py-2 pl-3 pr-2 text-left" scope="col">
              <span className="sr-only">{t('tasks.table.headers.statusToggle')}</span>
            </th>
            <th className="py-2 pr-3 text-left font-medium" scope="col">
              {t('tasks.table.headers.title')}
            </th>
            <th className="py-2 pr-3 text-left font-medium" scope="col">
              {t('tasks.table.headers.status')}
            </th>
            <th className="py-2 pr-3 text-left font-medium" scope="col">
              {t('tasks.table.headers.priority')}
            </th>
            <th className="py-2 pr-3 text-left font-medium" scope="col">
              {t('tasks.table.headers.due')}
            </th>
            <th className="py-2 pr-3 text-left font-medium" scope="col">
              {t('tasks.table.headers.tags')}
            </th>
            <th className="py-2 pr-3 text-left font-medium" scope="col">
              {t('tasks.table.headers.actions')}
            </th>
          </tr>
        </thead>
        <tbody>{children}</tbody>
      </table>
    </div>
  );
}

function LoadingState({ viewMode }: { viewMode: TaskViewMode }) {
  if (viewMode === 'table') {
    return (
      <div className="rounded-lg border bg-card p-3" aria-busy="true">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="mb-2 h-10 w-full last:mb-0" />
        ))}
      </div>
    );
  }
  if (viewMode === 'grid') {
    return (
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3" aria-busy="true">
        {Array.from({ length: 6 }).map((_, i) => (
          <Skeleton key={i} className="h-[116px] w-full" />
        ))}
      </div>
    );
  }
  return (
    <ul className="flex flex-col gap-3" aria-busy="true">
      {Array.from({ length: 4 }).map((_, i) => (
        <li key={i}>
          <Skeleton className="h-[92px] w-full" />
        </li>
      ))}
    </ul>
  );
}

function EmptyState() {
  const { t } = useTranslation();
  return (
    <div className="rounded-lg border border-dashed bg-card p-10 text-center">
      <p className="text-sm font-medium">{t('tasks.empty.title')}</p>
      <p className="mt-1 text-xs text-muted-foreground">{t('tasks.empty.subtitle')}</p>
    </div>
  );
}

function buildAnnouncements(tasks: readonly TaskDto[], t: TFunction): Announcements {
  const titleFor = (id: string | number) =>
    tasks.find((task) => task.id === String(id))?.title ?? String(id);

  return {
    onDragStart: ({ active }) => t('tasks.dnd.pickedUp', { title: titleFor(active.id) }),
    onDragOver: ({ active, over }) =>
      over
        ? t('tasks.dnd.movingOver', { title: titleFor(active.id), target: titleFor(over.id) })
        : t('tasks.dnd.noLongerOver', { title: titleFor(active.id) }),
    onDragEnd: ({ active, over }) =>
      over
        ? t('tasks.dnd.droppedOver', { title: titleFor(active.id), target: titleFor(over.id) })
        : t('tasks.dnd.droppedBack', { title: titleFor(active.id) }),
    onDragCancel: ({ active }) => t('tasks.dnd.cancelled', { title: titleFor(active.id) }),
  };
}
