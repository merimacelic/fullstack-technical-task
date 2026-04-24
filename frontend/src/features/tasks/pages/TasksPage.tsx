import { useState } from 'react';
import { Plus } from 'lucide-react';
import { toast } from 'sonner';

import { Button } from '@/shared/ui/button';
import { parseProblem } from '@/shared/lib/problemDetails';
import { useGetTagsQuery } from '@/features/tags/api';

import { useDeleteTaskMutation, useGetTasksQuery } from '../api';
import { useTaskFilters } from '../hooks/useTaskFilters';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { Pagination } from '../components/Pagination';
import { SortableTaskList } from '../components/SortableTaskList';
import { TaskFilters } from '../components/TaskFilters';
import { TaskForm } from '../components/TaskForm';
import { TaskList } from '../components/TaskList';
import type { TaskDto } from '../types';

export function TasksPage() {
  const { filters } = useTaskFilters();
  const { data, isFetching, isLoading, error } = useGetTasksQuery(filters);
  const { data: tags = [] } = useGetTagsQuery();
  const [deleteTask, { isLoading: deleting }] = useDeleteTaskMutation();

  const [formOpen, setFormOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<TaskDto | null>(null);
  const [pendingDelete, setPendingDelete] = useState<TaskDto | null>(null);

  const tasks = data?.items ?? [];
  const canDragReorder = filters.sortBy === 'Order';

  function openCreate() {
    setEditingTask(null);
    setFormOpen(true);
  }

  function openEdit(task: TaskDto) {
    setEditingTask(task);
    setFormOpen(true);
  }

  async function confirmDelete() {
    if (!pendingDelete) return;
    try {
      await deleteTask(pendingDelete.id).unwrap();
      toast.success('Task deleted.');
      setPendingDelete(null);
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(parsed.title, { description: parsed.detail });
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Tasks</h1>
          <p className="text-sm text-muted-foreground">
            Organise your work — filter, sort, and drag to reorder.
          </p>
        </div>
        <Button onClick={openCreate}>
          <Plus className="h-4 w-4" />
          New task
        </Button>
      </header>

      <TaskFilters />

      {error ? (
        <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-4 text-sm">
          <p className="font-medium">Couldn&apos;t load tasks.</p>
          <p className="text-muted-foreground">{parseProblem(error).detail}</p>
        </div>
      ) : canDragReorder && tasks.length > 0 ? (
        <SortableTaskList
          tasks={tasks}
          tags={tags}
          filters={filters}
          onEdit={openEdit}
          onDelete={setPendingDelete}
        />
      ) : (
        <TaskList
          tasks={tasks}
          tags={tags}
          isLoading={isLoading || isFetching}
          onEdit={openEdit}
          onDelete={setPendingDelete}
        />
      )}

      {data && (
        <Pagination
          page={data.page}
          pageSize={data.pageSize}
          totalCount={data.totalCount}
          totalPages={data.totalPages}
          hasPreviousPage={data.hasPreviousPage}
          hasNextPage={data.hasNextPage}
        />
      )}

      <TaskForm open={formOpen} onOpenChange={setFormOpen} task={editingTask} />
      <ConfirmDialog
        open={Boolean(pendingDelete)}
        onOpenChange={(v) => (v ? undefined : setPendingDelete(null))}
        title="Delete task?"
        description={
          pendingDelete
            ? `Delete “${pendingDelete.title}”? This cannot be undone.`
            : ''
        }
        confirmLabel="Delete"
        destructive
        isLoading={deleting}
        onConfirm={confirmDelete}
      />
    </div>
  );
}
