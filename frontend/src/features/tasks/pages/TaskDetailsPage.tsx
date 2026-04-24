import { useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Calendar, CheckCircle2, Pencil, RotateCcw, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

import { Button } from '@/shared/ui/button';
import { Skeleton } from '@/shared/ui/skeleton';
import { Badge } from '@/shared/ui/badge';
import { Separator } from '@/shared/ui/separator';
import { formatDateTime, formatRelative } from '@/shared/lib/date';
import { parseProblem } from '@/shared/lib/problemDetails';
import { useGetTagsQuery } from '@/features/tags/api';

import {
  useCompleteTaskMutation,
  useDeleteTaskMutation,
  useGetTaskByIdQuery,
  useReopenTaskMutation,
} from '../api';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { PriorityBadge } from '../components/PriorityBadge';
import { StatusBadge } from '../components/StatusBadge';
import { TaskForm } from '../components/TaskForm';

export function TaskDetailsPage() {
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
      toast.error(parsed.title, { description: parsed.detail });
    }
  }

  async function confirmDelete() {
    if (!task) return;
    try {
      await deleteTask(task.id).unwrap();
      toast.success('Task deleted.');
      navigate(-1);
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(parsed.title, { description: parsed.detail });
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
    const parsed = error ? parseProblem(error) : { title: 'Not found', detail: 'This task doesn\'t exist or you don\'t have access.' };
    return (
      <div className="flex flex-col items-start gap-3">
        <Button variant="ghost" asChild>
          <Link to="/tasks">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to tasks
          </Link>
        </Button>
        <div role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-4">
          <p className="text-sm font-medium">{parsed.title}</p>
          <p className="text-sm text-muted-foreground">{parsed.detail}</p>
        </div>
      </div>
    );
  }

  const taskTags = tags.filter((t) => task.tagIds.includes(t.id));
  const isCompleted = task.status === 'Completed';

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
          <ArrowLeft className="mr-1 h-4 w-4" />
          Back
        </Button>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setEditOpen(true)}>
            <Pencil className="mr-2 h-4 w-4" />
            Edit
          </Button>
          <Button
            variant="outline"
            onClick={toggleComplete}
            disabled={completing || reopening}
          >
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
          </Button>
          <Button variant="destructive" onClick={() => setDeleteOpen(true)}>
            <Trash2 className="mr-2 h-4 w-4" />
            Delete
          </Button>
        </div>
      </div>

      <article className="rounded-lg border bg-card p-6 shadow-sm">
        <h1 className="text-2xl font-semibold tracking-tight">{task.title}</h1>
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <StatusBadge status={task.status} />
          <PriorityBadge priority={task.priority} />
          {task.dueDateUtc && (
            <span className="inline-flex items-center gap-1 text-sm text-muted-foreground">
              <Calendar className="h-4 w-4" aria-hidden />
              Due {formatDateTime(task.dueDateUtc)}
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
          <p className="text-sm italic text-muted-foreground">No description.</p>
        )}

        <Separator className="my-6" />

        <dl className="grid grid-cols-1 gap-3 text-xs sm:grid-cols-3">
          <div>
            <dt className="text-muted-foreground">Created</dt>
            <dd title={formatDateTime(task.createdAtUtc)}>{formatRelative(task.createdAtUtc)}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Updated</dt>
            <dd title={formatDateTime(task.updatedAtUtc)}>{formatRelative(task.updatedAtUtc)}</dd>
          </div>
          {task.completedAtUtc && (
            <div>
              <dt className="text-muted-foreground">Completed</dt>
              <dd title={formatDateTime(task.completedAtUtc)}>{formatRelative(task.completedAtUtc)}</dd>
            </div>
          )}
        </dl>
      </article>

      <TaskForm open={editOpen} onOpenChange={setEditOpen} task={task} />
      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title="Delete task?"
        description={`Delete “${task.title}”? This cannot be undone.`}
        confirmLabel="Delete"
        destructive
        isLoading={deleting}
        onConfirm={confirmDelete}
      />
    </div>
  );
}
