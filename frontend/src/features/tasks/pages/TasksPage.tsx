import { useState } from 'react';
import { Plus, Tags } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { Button } from '@/shared/ui/button';
import { parseProblem, problemDetail, problemTitle } from '@/shared/lib/problemDetails';
import { useGetTagsQuery } from '@/features/tags/api';
import { TagManager } from '@/features/tags/components/TagManager';

import { useDeleteTaskMutation, useGetTasksQuery } from '../api';
import { useTaskFilters } from '../hooks/useTaskFilters';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { Pagination } from '../components/Pagination';
import { TaskFilters } from '../components/TaskFilters';
import { TaskForm } from '../components/TaskForm';
import { TaskView } from '../components/TaskView';
import { ViewModeToggle } from '../components/ViewModeToggle';
import type { TaskDto } from '../types';

export function TasksPage() {
  const { t } = useTranslation();
  const { filters, viewMode, setViewMode } = useTaskFilters();
  const { data, isFetching, isLoading, error } = useGetTasksQuery(filters);
  const { data: tags = [] } = useGetTagsQuery();
  const [deleteTask, { isLoading: deleting }] = useDeleteTaskMutation();

  const [formOpen, setFormOpen] = useState(false);
  const [tagsOpen, setTagsOpen] = useState(false);
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
      toast.success(t('tasks.toast.deleted'));
      setPendingDelete(null);
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t('tasks.page.title')}</h1>
          <p className="text-sm text-muted-foreground">{t('tasks.page.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <ViewModeToggle value={viewMode} onChange={setViewMode} />
          <Button variant="outline" onClick={() => setTagsOpen(true)}>
            <Tags className="h-4 w-4" />
            <span className="hidden sm:inline">{t('tasks.page.tagsButton')}</span>
          </Button>
          <Button onClick={openCreate}>
            <Plus className="h-4 w-4" />
            {t('tasks.page.newTask')}
          </Button>
        </div>
      </header>

      <TaskFilters />

      {error ? (
        <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-4 text-sm">
          <p className="font-medium">{t('tasks.errors.loadFailed')}</p>
          <p className="text-muted-foreground">
            {(() => {
              const p = parseProblem(error);
              return t(problemDetail(p));
            })()}
          </p>
        </div>
      ) : (
        <TaskView
          tasks={tasks}
          tags={tags}
          viewMode={viewMode}
          draggable={canDragReorder && tasks.length > 0}
          filters={filters}
          hasPreviousPage={data?.hasPreviousPage ?? false}
          hasNextPage={data?.hasNextPage ?? false}
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
      <TagManager open={tagsOpen} onOpenChange={setTagsOpen} />
      <ConfirmDialog
        open={Boolean(pendingDelete)}
        onOpenChange={(v) => (v ? undefined : setPendingDelete(null))}
        title={t('tasks.confirm.delete.title')}
        description={
          pendingDelete
            ? t('tasks.confirm.delete.description', { title: pendingDelete.title })
            : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        isLoading={deleting}
        onConfirm={confirmDelete}
      />
    </div>
  );
}
