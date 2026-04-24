import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Check, Pencil, Trash2, X } from 'lucide-react';
import { toast } from 'sonner';

import { Button } from '@/shared/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog';
import { Input } from '@/shared/ui/input';
import { FormField } from '@/shared/ui/form-field';
import { Skeleton } from '@/shared/ui/skeleton';
import { ConfirmDialog } from '@/features/tasks/components/ConfirmDialog';
import { parseProblem } from '@/shared/lib/problemDetails';

import {
  useCreateTagMutation,
  useDeleteTagMutation,
  useGetTagsQuery,
  useRenameTagMutation,
} from '../api';
import { tagFormSchema, type TagFormValues } from '../schemas';
import type { TagDto } from '../types';

interface TagManagerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function TagManager({ open, onOpenChange }: TagManagerProps) {
  const { data: tags, isLoading } = useGetTagsQuery(undefined, { skip: !open });
  const [createTag, { isLoading: creating }] = useCreateTagMutation();
  const [renameTag, { isLoading: renaming }] = useRenameTagMutation();
  const [deleteTag, { isLoading: deleting }] = useDeleteTagMutation();

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingName, setEditingName] = useState('');
  const [pendingDelete, setPendingDelete] = useState<TagDto | null>(null);

  const form = useForm<TagFormValues>({
    resolver: zodResolver(tagFormSchema),
    defaultValues: { name: '' },
    mode: 'onBlur',
  });

  async function onCreate(values: TagFormValues) {
    try {
      await createTag({ name: values.name }).unwrap();
      toast.success('Tag created.');
      form.reset({ name: '' });
    } catch (err) {
      const parsed = parseProblem(err as never);
      if (parsed.status === 409) {
        form.setError('name', { message: parsed.detail });
      } else if (parsed.fieldErrors?.['Name']?.[0]) {
        form.setError('name', { message: parsed.fieldErrors['Name'][0] });
      } else {
        toast.error(parsed.title, { description: parsed.detail });
      }
    }
  }

  async function saveRename(tag: TagDto) {
    const name = editingName.trim();
    if (!name || name === tag.name) {
      setEditingId(null);
      return;
    }
    try {
      await renameTag({ id: tag.id, name }).unwrap();
      toast.success('Tag renamed.');
      setEditingId(null);
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(parsed.title, { description: parsed.detail });
    }
  }

  async function confirmDelete() {
    if (!pendingDelete) return;
    try {
      await deleteTag(pendingDelete.id).unwrap();
      toast.success('Tag deleted.');
      setPendingDelete(null);
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(parsed.title, { description: parsed.detail });
    }
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Tags</DialogTitle>
            <DialogDescription>Create, rename, or remove tags. Tags are scoped to your account.</DialogDescription>
          </DialogHeader>

          <form
            onSubmit={form.handleSubmit(onCreate)}
            className="flex items-end gap-2"
            noValidate
          >
            <div className="flex-1">
              <FormField
                id="tag-name"
                label="Add a tag"
                error={form.formState.errors.name?.message}
              >
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    placeholder="e.g. Home"
                    aria-invalid={invalid}
                    aria-describedby={describedBy}
                    {...form.register('name')}
                  />
                )}
              </FormField>
            </div>
            <Button type="submit" disabled={creating}>
              {creating ? 'Adding…' : 'Add'}
            </Button>
          </form>

          <div className="max-h-[320px] overflow-y-auto rounded-md border">
            {isLoading ? (
              <ul className="divide-y">
                {Array.from({ length: 3 }).map((_, i) => (
                  <li key={i} className="p-3">
                    <Skeleton className="h-4 w-32" />
                  </li>
                ))}
              </ul>
            ) : tags && tags.length > 0 ? (
              <ul className="divide-y">
                {tags.map((tag) => (
                  <li key={tag.id} className="flex items-center gap-2 p-3">
                    {editingId === tag.id ? (
                      <div className="flex flex-1 items-center gap-2">
                        <Input
                          ref={(el) => el?.focus()}
                          value={editingName}
                          onChange={(e) => setEditingName(e.target.value)}
                          aria-label={`Rename tag ${tag.name}`}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') {
                              e.preventDefault();
                              void saveRename(tag);
                            } else if (e.key === 'Escape') {
                              setEditingId(null);
                            }
                          }}
                        />
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => void saveRename(tag)}
                          disabled={renaming}
                          aria-label="Save"
                        >
                          <Check className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => setEditingId(null)}
                          aria-label="Cancel rename"
                        >
                          <X className="h-4 w-4" />
                        </Button>
                      </div>
                    ) : (
                      <>
                        <span className="flex-1 truncate text-sm">{tag.name}</span>
                        <span className="text-xs text-muted-foreground">
                          {tag.taskCount} task{tag.taskCount === 1 ? '' : 's'}
                        </span>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => {
                            setEditingId(tag.id);
                            setEditingName(tag.name);
                          }}
                          aria-label={`Rename ${tag.name}`}
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => setPendingDelete(tag)}
                          aria-label={`Delete ${tag.name}`}
                        >
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </>
                    )}
                  </li>
                ))}
              </ul>
            ) : (
              <p className="p-6 text-center text-sm text-muted-foreground">
                No tags yet — add one above to get started.
              </p>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Close
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={Boolean(pendingDelete)}
        onOpenChange={(v) => (v ? undefined : setPendingDelete(null))}
        title="Delete tag?"
        description={
          pendingDelete
            ? `This will remove “${pendingDelete.name}” from ${pendingDelete.taskCount} task${
                pendingDelete.taskCount === 1 ? '' : 's'
              }. Tasks themselves aren't deleted.`
            : ''
        }
        confirmLabel="Delete"
        destructive
        isLoading={deleting}
        onConfirm={confirmDelete}
      />
    </>
  );
}
