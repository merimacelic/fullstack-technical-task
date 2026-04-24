import { useState } from 'react';
import { Check, ChevronsUpDown, Plus, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { Button } from '@/shared/ui/button';
import { Badge } from '@/shared/ui/badge';
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/shared/ui/command';
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/ui/popover';
import { cn } from '@/shared/lib/cn';
import { parseProblem, problemDetail, problemTitle } from '@/shared/lib/problemDetails';
import { useCreateTagMutation, useGetTagsQuery } from '../api';

interface TagPickerProps {
  value: string[];
  onChange: (value: string[]) => void;
  id?: string;
  'aria-describedby'?: string;
  'aria-invalid'?: boolean;
  disabled?: boolean;
}

export function TagPicker({ value, onChange, id, disabled, ...aria }: TagPickerProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const { data: tags = [], isLoading } = useGetTagsQuery();
  const [createTag, { isLoading: creating }] = useCreateTagMutation();

  const selectedTags = tags.filter((tag) => value.includes(tag.id));
  const filtered = tags.filter((tag) =>
    tag.name.toLowerCase().includes(search.trim().toLowerCase()),
  );
  const canCreate =
    search.trim().length > 0 &&
    !tags.some((tag) => tag.name.toLowerCase() === search.trim().toLowerCase());

  function toggle(tagId: string) {
    onChange(value.includes(tagId) ? value.filter((x) => x !== tagId) : [...value, tagId]);
  }

  async function handleCreate() {
    const name = search.trim();
    if (!name) return;
    try {
      const created = await createTag({ name }).unwrap();
      onChange([...value, created.id]);
      setSearch('');
    } catch (err) {
      const parsed = parseProblem(err as never);
      toast.error(t(problemTitle(parsed)), { description: t(problemDetail(parsed)) });
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            id={id}
            type="button"
            variant="outline"
            role="combobox"
            aria-expanded={open}
            aria-haspopup="listbox"
            aria-invalid={aria['aria-invalid']}
            aria-describedby={aria['aria-describedby']}
            disabled={disabled}
            className={cn('w-full justify-between', value.length === 0 && 'text-muted-foreground')}
          >
            {value.length === 0
              ? t('tags.picker.empty')
              : t('tags.picker.selected', { count: value.length })}
            <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-[min(380px,calc(100vw-2rem))] p-0" align="start">
          <Command shouldFilter={false}>
            <CommandInput
              placeholder={t('tags.picker.searchPlaceholder')}
              value={search}
              onValueChange={setSearch}
            />
            <CommandList>
              {!isLoading && filtered.length === 0 && !canCreate && (
                <CommandEmpty>{t('tags.picker.emptyText')}</CommandEmpty>
              )}
              <CommandGroup>
                {filtered.map((tag) => {
                  const selected = value.includes(tag.id);
                  return (
                    <CommandItem key={tag.id} value={tag.id} onSelect={() => toggle(tag.id)}>
                      <Check className={cn('mr-2 h-4 w-4', selected ? 'opacity-100' : 'opacity-0')} />
                      {tag.name}
                      <span className="ml-auto text-xs text-muted-foreground">
                        {tag.taskCount}
                      </span>
                    </CommandItem>
                  );
                })}
              </CommandGroup>
              {canCreate && (
                <CommandGroup heading={t('tags.picker.createHeading')}>
                  <CommandItem value={`__create__${search}`} onSelect={() => void handleCreate()}>
                    <Plus className="mr-2 h-4 w-4" />
                    {t('tags.picker.createItem', { name: search.trim() })}
                    {creating && t('tags.picker.creating')}
                  </CommandItem>
                </CommandGroup>
              )}
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>

      {selectedTags.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {selectedTags.map((tag) => (
            <Badge key={tag.id} variant="secondary" className="gap-1 text-xs">
              {tag.name}
              <button
                type="button"
                onClick={() => toggle(tag.id)}
                className="rounded-full outline-none hover:bg-muted focus-visible:ring-2 focus-visible:ring-ring"
                aria-label={t('tags.picker.removeAria', { name: tag.name })}
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
        </div>
      )}
    </div>
  );
}
