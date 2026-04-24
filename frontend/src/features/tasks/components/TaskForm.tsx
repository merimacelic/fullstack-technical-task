import { useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Textarea } from '@/shared/ui/textarea';
import { FormField } from '@/shared/ui/form-field';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/ui/select';
import { DatePicker } from '@/shared/ui/date-picker';
import { TagPicker } from '@/features/tags/components/TagPicker';
import { parseProblem } from '@/shared/lib/problemDetails';
import { dateInputToIsoUtc, toDateInputValue } from '@/shared/lib/date';
import { useCreateTaskMutation, useUpdateTaskMutation } from '../api';
import { taskFormSchema, type TaskFormValues } from '../schemas';
import { TASK_PRIORITIES, type TaskDto } from '../types';

interface TaskFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  task?: TaskDto | null;
}

export function TaskForm({ open, onOpenChange, task }: TaskFormProps) {
  const isEditing = Boolean(task);
  const [createTask, { isLoading: creating }] = useCreateTaskMutation();
  const [updateTask, { isLoading: updating }] = useUpdateTaskMutation();
  const submitting = creating || updating;

  const form = useForm<TaskFormValues>({
    resolver: zodResolver(taskFormSchema),
    defaultValues: buildDefaults(task),
    mode: 'onBlur',
  });

  // Reset form state whenever the modal opens or the task prop changes.
  useEffect(() => {
    if (open) form.reset(buildDefaults(task));
  }, [open, task, form]);

  async function onSubmit(values: TaskFormValues) {
    try {
      const payload = {
        title: values.title,
        description: values.description?.trim() || null,
        priority: values.priority,
        dueDateUtc: values.dueDate ? dateInputToIsoUtc(values.dueDate) : null,
        tagIds: values.tagIds ?? [],
      };

      if (task) {
        await updateTask({ id: task.id, ...payload }).unwrap();
        toast.success('Task updated.');
      } else {
        await createTask(payload).unwrap();
        toast.success('Task created.');
      }
      onOpenChange(false);
    } catch (err) {
      const parsed = parseProblem(err as never);
      if (parsed.fieldErrors) {
        for (const [field, messages] of Object.entries(parsed.fieldErrors)) {
          const msg = messages?.[0];
          if (!msg) continue;
          // Server uses PascalCase field names; map to camelCase if needed.
          const key = mapFieldName(field);
          if (key) form.setError(key, { message: msg });
        }
        toast.error(parsed.title, { description: parsed.detail });
        return;
      }
      toast.error(parsed.title, { description: parsed.detail });
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit task' : 'New task'}</DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Update the task details below.'
              : 'Add a new task to your list. You can attach tags and set a priority.'}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
          <FormField id="title" label="Title" required error={form.formState.errors.title?.message}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                autoComplete="off"
                aria-invalid={invalid}
                aria-describedby={describedBy}
                {...form.register('title')}
              />
            )}
          </FormField>

          <FormField
            id="description"
            label="Description"
            error={form.formState.errors.description?.message}
          >
            {({ id, describedBy, invalid }) => (
              <Textarea
                id={id}
                rows={4}
                aria-invalid={invalid}
                aria-describedby={describedBy}
                {...form.register('description')}
              />
            )}
          </FormField>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField
              id="priority"
              label="Priority"
              required
              error={form.formState.errors.priority?.message}
            >
              {({ id, describedBy, invalid }) => (
                <Controller
                  control={form.control}
                  name="priority"
                  render={({ field }) => (
                    <Select value={field.value} onValueChange={field.onChange}>
                      <SelectTrigger
                        id={id}
                        aria-invalid={invalid}
                        aria-describedby={describedBy}
                      >
                        <SelectValue placeholder="Select priority" />
                      </SelectTrigger>
                      <SelectContent>
                        {TASK_PRIORITIES.map((p) => (
                          <SelectItem key={p} value={p}>
                            {p}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              )}
            </FormField>

            <FormField
              id="dueDate"
              label="Due date"
              error={form.formState.errors.dueDate?.message}
            >
              {({ id, describedBy, invalid }) => (
                <Controller
                  control={form.control}
                  name="dueDate"
                  render={({ field }) => (
                    <DatePicker
                      id={id}
                      aria-invalid={invalid}
                      aria-describedby={describedBy}
                      value={field.value ? new Date(field.value) : undefined}
                      onChange={(date) => field.onChange(date ? toDateInputValue(date) : '')}
                      disabledBefore={new Date(new Date().setHours(0, 0, 0, 0))}
                    />
                  )}
                />
              )}
            </FormField>
          </div>

          <FormField id="tagIds" label="Tags" error={form.formState.errors.tagIds?.message}>
            {({ id, describedBy, invalid }) => (
              <Controller
                control={form.control}
                name="tagIds"
                render={({ field }) => (
                  <TagPicker
                    id={id}
                    aria-invalid={invalid}
                    aria-describedby={describedBy}
                    value={field.value ?? []}
                    onChange={field.onChange}
                  />
                )}
              />
            )}
          </FormField>

          <DialogFooter className="pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={submitting}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={submitting} aria-busy={submitting}>
              {submitting ? 'Saving…' : isEditing ? 'Save changes' : 'Create task'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function buildDefaults(task: TaskDto | null | undefined): TaskFormValues {
  if (!task) {
    return {
      title: '',
      description: '',
      priority: 'Medium',
      dueDate: '',
      tagIds: [],
    };
  }
  return {
    title: task.title,
    description: task.description ?? '',
    priority: task.priority,
    dueDate: task.dueDateUtc ? toDateInputValue(task.dueDateUtc) : '',
    tagIds: [...task.tagIds],
  };
}

function mapFieldName(serverField: string): keyof TaskFormValues | null {
  const lower = serverField.toLowerCase();
  if (lower.startsWith('title')) return 'title';
  if (lower.startsWith('description')) return 'description';
  if (lower.startsWith('priority')) return 'priority';
  if (lower.startsWith('duedate')) return 'dueDate';
  if (lower.startsWith('tag')) return 'tagIds';
  return null;
}
