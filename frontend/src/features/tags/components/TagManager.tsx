import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Check, Pencil, Trash2, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
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
import { parseProblem, problemDetail, problemTitle } from '@/shared/lib/problemDetails';

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
  const { t } = useTranslation();
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
    // Same reasoning as TaskForm: blur-mode validation inserts an error and
    // shifts layout when focus leaves the input to click Close / ×, so the
    // close affordance moves out from under the cursor before mouseup fires.
    mode: 'onSubmit',
    reValidateMode: 'onChange',
  });

  // Clear residual state when the dialog is reopened — the form hook instance
  // persists across open/close, so a prior error would otherwise show up again.
  useEffect(() => {
    if (open) form.reset({ name: '' });
  }, [open, form]);

  async function onCreate(values: TagFormValues) {
    try {
      await createTag({ name: values.name }).unwrap();
      toast.success(t('tags.manager.toast.created'));
      form.reset({ name: '' });
    } catch (err) {
      const parsed = parseProblem(err as never);
      if (parsed.status === 409) {
        form.setError('name', { message: parsed.detail || t(parsed.detailKey) });
      } else if (parsed.fieldErrors?.['Name']?.[0]) {
        form.setError('name', { message: parsed.fieldErrors['Name'][0] });
      } else {
        toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
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
      toast.success(t('tags.manager.toast.renamed'));
      setEditingId(null);
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
    }
  }

  async function confirmDelete() {
    if (!pendingDelete) return;
    try {
      await deleteTag(pendingDelete.id).unwrap();
      toast.success(t('tags.manager.toast.deleted'));
      setPendingDelete(null);
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
    }
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('tags.manager.title')}</DialogTitle>
            <DialogDescription>{t('tags.manager.description')}</DialogDescription>
          </DialogHeader>

          <form
            onSubmit={form.handleSubmit(onCreate)}
            className="flex items-end gap-2"
            noValidate
          >
            <div className="flex-1">
              <FormField
                id="tag-name"
                label={t('tags.manager.addLabel')}
                error={form.formState.errors.name?.message}
              >
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    placeholder={t('tags.manager.placeholder')}
                    aria-invalid={invalid}
                    aria-describedby={describedBy}
                    {...form.register('name')}
                  />
                )}
              </FormField>
            </div>
            <Button type="submit" disabled={creating}>
              {creating ? t('tags.manager.adding') : t('tags.manager.add')}
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
                          aria-label={t('tags.manager.aria.rename', { name: tag.name })}
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
                          aria-label={t('tags.manager.aria.save')}
                        >
                          <Check className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => setEditingId(null)}
                          aria-label={t('tags.manager.aria.cancel')}
                        >
                          <X className="h-4 w-4" />
                        </Button>
                      </div>
                    ) : (
                      <>
                        <span className="flex-1 truncate text-sm">{tag.name}</span>
                        <span className="text-xs text-muted-foreground">
                          {t('tags.manager.taskCount', { count: tag.taskCount })}
                        </span>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => {
                            setEditingId(tag.id);
                            setEditingName(tag.name);
                          }}
                          aria-label={t('tags.manager.aria.renameOne', { name: tag.name })}
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => setPendingDelete(tag)}
                          aria-label={t('tags.manager.aria.delete', { name: tag.name })}
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
                {t('tags.manager.empty')}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              {t('tags.manager.close')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={Boolean(pendingDelete)}
        onOpenChange={(v) => (v ? undefined : setPendingDelete(null))}
        title={t('tags.manager.confirmDelete.title')}
        description={
          pendingDelete
            ? t('tags.manager.confirmDelete.description', {
                name: pendingDelete.name,
                count: pendingDelete.taskCount,
              })
            : ''
        }
        confirmLabel={t('tags.manager.confirmDelete.confirm')}
        destructive
        isLoading={deleting}
        onConfirm={confirmDelete}
      />
    </>
  );
}
