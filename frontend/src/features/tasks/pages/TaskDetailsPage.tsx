import { useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Calendar, CheckCircle2, Pencil, RotateCcw, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { Button } from '@/shared/ui/button';
import { Skeleton } from '@/shared/ui/skeleton';
import { Badge } from '@/shared/ui/badge';
import { Separator } from '@/shared/ui/separator';
import { formatDateTime, formatRelative } from '@/shared/lib/date';
import { parseProblem, problemDetail, problemTitle } from '@/shared/lib/problemDetails';
import { useGetTagsQuery } from '@/features/tags/api';

import {
  useCompleteTaskMutation,
  useDeleteTaskMutation,
  useGetTaskByIdQuery,
  useReopenTaskMutation,
} from '../api';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { PriorityPicker } from '../components/PriorityPicker';
import { StatusPicker } from '../components/StatusPicker';
import { TaskForm } from '../components/TaskForm';

export function TaskDetailsPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: task, isLoading, error } = useGetTaskByIdQuery(id!, { skip: !id });
  const { data: tags = [] } = useGetTagsQuery();
  const [complete, { isLoading: completing }] = useCompleteTaskMutation();
  const [reopen, { isLoading: reopening }] = useReopenTaskMutation();
  const [deleteTask, { isLoading: deleting }] = useDeleteTaskMutation();

  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  async function toggleComplete() {
    if (!task) return;
    try {
      if (task.status === 'Completed') {
        await reopen(task.id).unwrap();
      } else {
        await complete(task.id).unwrap();
      }
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
    }
  }

  async function confirmDelete() {
    if (!task) return;
    try {
      await deleteTask(task.id).unwrap();
      toast.success(t('tasks.toast.deleted'));
      navigate(-1);
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
    }
  }

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4" aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-32 w-full" />
      </div>
    );
  }

  if (error || !task) {
    const parsed = error ? parseProblem(error) : null;
    const title = parsed ? t(problemTitle(parsed)) : t('tasks.details.notFound.title');
    const detail = parsed ? t(problemDetail(parsed)) : t('tasks.details.notFound.description');
    return (
      <div className="flex flex-col items-start gap-3">
        <Button variant="ghost" asChild>
          <Link to="/tasks">
            <ArrowLeft className="mr-2 h-4 w-4" />
            {t('common.backToTasks')}
          </Link>
        </Button>
        <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-4">
          <p className="text-sm font-medium">{title}</p>
          <p className="text-sm text-muted-foreground">{detail}</p>
        </div>
      </div>
    );
  }

  const taskTags = tags.filter((tag) => task.tagIds.includes(tag.id));
  const isCompleted = task.status === 'Completed';

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
          <ArrowLeft className="mr-1 h-4 w-4" />
          {t('tasks.details.back')}
        </Button>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setEditOpen(true)}>
            <Pencil className="mr-2 h-4 w-4" />
            {t('tasks.details.edit')}
          </Button>
          <Button
            variant="outline"
            onClick={toggleComplete}
            disabled={completing || reopening}
          >
            {isCompleted ? (
              <>
                <RotateCcw className="mr-2 h-4 w-4" />
                {t('tasks.details.reopen')}
              </>
            ) : (
              <>
                <CheckCircle2 className="mr-2 h-4 w-4" />
                {t('tasks.details.markComplete')}
              </>
            )}
          </Button>
          <Button variant="destructive" onClick={() => setDeleteOpen(true)}>
            <Trash2 className="mr-2 h-4 w-4" />
            {t('tasks.details.delete')}
          </Button>
        </div>
      </div>

      <article className="rounded-lg border bg-card p-6 shadow-sm">
        <h1 className="text-2xl font-semibold tracking-tight">{task.title}</h1>
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <StatusPicker taskId={task.id} status={task.status} variant="pill" />
          <PriorityPicker taskId={task.id} priority={task.priority} />
          {task.dueDateUtc && (
            <span className="inline-flex items-center gap-1 text-sm text-muted-foreground">
              <Calendar className="h-4 w-4" aria-hidden />
              {t('tasks.details.due', { when: formatDateTime(task.dueDateUtc) })}
            </span>
          )}
        </div>
        {taskTags.length > 0 && (
          <div className="mt-3 flex flex-wrap gap-1.5">
            {taskTags.map((tag) => (
              <Badge key={tag.id} variant="secondary">
                #{tag.name}
              </Badge>
            ))}
          </div>
        )}

        <Separator className="my-6" />

        {task.description ? (
          <p className="whitespace-pre-wrap text-sm leading-relaxed">{task.description}</p>
        ) : (
          <p className="text-sm italic text-muted-foreground">{t('tasks.details.noDescription')}</p>
        )}

        <Separator className="my-6" />

        <dl className="grid grid-cols-1 gap-3 text-xs sm:grid-cols-3">
          <div>
            <dt className="text-muted-foreground">{t('tasks.details.created')}</dt>
            <dd title={formatDateTime(task.createdAtUtc)}>{formatRelative(task.createdAtUtc)}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t('tasks.details.updated')}</dt>
            <dd title={formatDateTime(task.updatedAtUtc)}>{formatRelative(task.updatedAtUtc)}</dd>
          </div>
          {task.completedAtUtc && (
            <div>
              <dt className="text-muted-foreground">{t('tasks.details.completed')}</dt>
              <dd title={formatDateTime(task.completedAtUtc)}>{formatRelative(task.completedAtUtc)}</dd>
            </div>
          )}
        </dl>
      </article>

      <TaskForm open={editOpen} onOpenChange={setEditOpen} task={task} />
      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title={t('tasks.confirm.delete.title')}
        description={t('tasks.confirm.delete.description', { title: task.title })}
        confirmLabel={t('common.delete')}
        destructive
        isLoading={deleting}
        onConfirm={confirmDelete}
      />
    </div>
  );
}
