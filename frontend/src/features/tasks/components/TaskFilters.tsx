import { useEffect, useMemo, useState } from 'react';
import { ArrowDownAZ, ArrowDownUp, Filter, Search, SlidersHorizontal } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Label } from '@/shared/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/ui/select';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '@/shared/ui/sheet';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/shared/ui/tooltip';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { useGetTagsQuery } from '@/features/tags/api';
import { useTaskFilters } from '../hooks/useTaskFilters';
import { TASK_PRIORITIES, TASK_STATUSES, type TaskPriority, type TaskStatus } from '../types';
import { MultiFilterSelect, type MultiFilterOption } from './MultiFilterSelect';

const SORT_KEYS = ['Order', 'Title', 'CreatedAt', 'UpdatedAt', 'DueDate', 'Priority'] as const;

interface TaskFiltersProps {
  actions?: React.ReactNode;
}

export function TaskFilters({ actions }: TaskFiltersProps) {
  const { t } = useTranslation();
  const { filters, setFilter } = useTaskFilters();
  const { data: tags } = useGetTagsQuery();

  const [searchDraft, setSearchDraft] = useState(filters.search ?? '');
  const debouncedSearch = useDebouncedValue(searchDraft, 300);

  useEffect(() => {
    if ((filters.search ?? '') !== debouncedSearch) {
      setFilter('search', debouncedSearch || undefined);
    }
    // intentionally not depending on filters.search — we own the write here
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, setFilter]);

  useEffect(() => {
    setSearchDraft(filters.search ?? '');
  }, [filters.search]);

  const searchInput = (
    <div className="relative w-full md:flex-1">
      <Search
        className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden
      />
      <Input
        type="search"
        placeholder={t('tasks.filters.search')}
        value={searchDraft}
        onChange={(e) => setSearchDraft(e.target.value)}
        aria-label={t('tasks.filters.searchAria')}
        className="w-full pl-8"
      />
    </div>
  );

  return (
    <div className="flex flex-col gap-3">
      <div className="hidden flex-wrap items-center gap-2 md:flex">
        {searchInput}

        <VerticalSeparator />

        <GroupIcon icon={Filter} label={t('tasks.filters.filterBy')} />
        <StatusSelect value={filters.statuses} onChange={(v) => setFilter('statuses', v)} />
        <PrioritySelect
          value={filters.priorities}
          onChange={(v) => setFilter('priorities', v)}
        />
        <TagSelect
          value={filters.tagIds}
          onChange={(v) => setFilter('tagIds', v)}
          tags={tags ?? []}
        />

        <VerticalSeparator />

        <GroupIcon icon={ArrowDownUp} label={t('tasks.filters.sortBy')} />
        <SortControls />

        {actions && <div className="ml-auto flex items-center gap-2">{actions}</div>}
      </div>

      <div className="flex flex-col gap-2 md:hidden">
        {searchInput}
        <div className="flex items-center gap-2">
          <Sheet>
            <SheetTrigger asChild>
              <Button variant="outline" size="sm">
                <SlidersHorizontal className="mr-2 h-4 w-4" />
                {t('tasks.filters.mobileButton')}
              </Button>
            </SheetTrigger>
            <SheetContent side="bottom" className="max-h-[85vh] overflow-y-auto">
              <SheetHeader>
                <SheetTitle>{t('tasks.filters.mobileButton')}</SheetTitle>
              </SheetHeader>
              <div className="mt-4 flex flex-col gap-4">
                <Field label={t('tasks.filters.placeholders.status')}>
                  <StatusSelect
                    value={filters.statuses}
                    onChange={(v) => setFilter('statuses', v)}
                  />
                </Field>
                <Field label={t('tasks.filters.placeholders.priority')}>
                  <PrioritySelect
                    value={filters.priorities}
                    onChange={(v) => setFilter('priorities', v)}
                  />
                </Field>
                <Field label={t('tasks.filters.placeholders.tag')}>
                  <TagSelect
                    value={filters.tagIds}
                    onChange={(v) => setFilter('tagIds', v)}
                    tags={tags ?? []}
                  />
                </Field>
                <Field label={t('tasks.filters.placeholders.sort')}>
                  <SortControls />
                </Field>
              </div>
            </SheetContent>
          </Sheet>

          {actions && <div className="ml-auto flex items-center gap-2">{actions}</div>}
        </div>
      </div>
    </div>
  );
}

function GroupIcon({ icon: Icon, label }: { icon: typeof Filter; label: string }) {
  return (
    <span
      className="inline-flex h-4 w-4 items-center justify-center text-muted-foreground"
      aria-label={label}
      title={label}
    >
      <Icon className="h-4 w-4" aria-hidden />
    </span>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label className="text-xs uppercase tracking-wide text-muted-foreground">{label}</Label>
      {children}
    </div>
  );
}

function VerticalSeparator() {
  return <span className="mx-1 hidden h-6 w-px bg-border md:inline-block" aria-hidden />;
}

function StatusSelect({
  value,
  onChange,
}: {
  value: TaskStatus[] | undefined;
  onChange: (value: TaskStatus[] | undefined) => void;
}) {
  const { t } = useTranslation();
  const options = useMemo<readonly MultiFilterOption<TaskStatus>[]>(
    () => TASK_STATUSES.map((s) => ({ value: s, label: t(`tasks.status.${s}`) })),
    [t],
  );
  return (
    <MultiFilterSelect
      value={value}
      onChange={onChange}
      options={options}
      placeholder={t('tasks.filters.placeholders.status')}
      ariaLabel={t('tasks.filters.aria.statusFilter')}
      clearAriaLabel={t('tasks.filters.aria.clearStatus')}
    />
  );
}

function PrioritySelect({
  value,
  onChange,
}: {
  value: TaskPriority[] | undefined;
  onChange: (value: TaskPriority[] | undefined) => void;
}) {
  const { t } = useTranslation();
  const options = useMemo<readonly MultiFilterOption<TaskPriority>[]>(
    () => TASK_PRIORITIES.map((p) => ({ value: p, label: t(`tasks.priority.${p}`) })),
    [t],
  );
  return (
    <MultiFilterSelect
      value={value}
      onChange={onChange}
      options={options}
      placeholder={t('tasks.filters.placeholders.priority')}
      ariaLabel={t('tasks.filters.aria.priorityFilter')}
      clearAriaLabel={t('tasks.filters.aria.clearPriority')}
    />
  );
}

function TagSelect({
  value,
  onChange,
  tags,
}: {
  value: string[] | undefined;
  onChange: (value: string[] | undefined) => void;
  tags: readonly { id: string; name: string }[];
}) {
  const { t } = useTranslation();
  // Tag lists can grow arbitrarily; enable in-popover search once there are
  // enough to make scanning slow.
  const options = useMemo<readonly MultiFilterOption<string>[]>(
    () => tags.map((tag) => ({ value: tag.id, label: tag.name })),
    [tags],
  );
  return (
    <MultiFilterSelect
      value={value}
      onChange={onChange}
      options={options}
      placeholder={t('tasks.filters.placeholders.tag')}
      ariaLabel={t('tasks.filters.aria.tagFilter')}
      clearAriaLabel={t('tasks.filters.aria.clearTag')}
      searchable={options.length > 6}
      searchPlaceholder={t('tasks.filters.tag.searchPlaceholder')}
      emptyText={t('tasks.filters.tag.emptyText')}
    />
  );
}

function SortControls() {
  const { t } = useTranslation();
  const { filters, setFilter } = useTaskFilters();
  const ascending = filters.sortDirection === 'Ascending';
  // Manual order has a user-set direction implicit in the drag; a toggle here
  // would just invert what they arranged by hand.
  const directionDisabled = filters.sortBy === 'Order';
  const directionLabel = ascending
    ? t('tasks.filters.sort.directionAscending')
    : t('tasks.filters.sort.directionDescending');
  return (
    <div className="flex items-center gap-1.5">
      <Select value={filters.sortBy} onValueChange={(v) => setFilter('sortBy', v as never)}>
        <SelectTrigger className="w-[150px]" aria-label={t('tasks.filters.sortBy')}>
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {SORT_KEYS.map((key) => (
            <SelectItem key={key} value={key}>
              {t(`tasks.filters.sort.${key}`)}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="outline"
            size="icon"
            className="h-9 w-9 cursor-pointer"
            disabled={directionDisabled}
            aria-label={t('tasks.filters.aria.sortDirection', { direction: directionLabel })}
            onClick={() => setFilter('sortDirection', ascending ? 'Descending' : 'Ascending')}
          >
            <ArrowDownAZ
              className={`h-4 w-4 transition-transform ${ascending ? '' : 'rotate-180'}`}
              aria-hidden
            />
          </Button>
        </TooltipTrigger>
        <TooltipContent side="bottom">
          {directionDisabled
            ? t('tasks.filters.sort.manual')
            : ascending
              ? t('tasks.filters.sort.ascending')
              : t('tasks.filters.sort.descending')}
        </TooltipContent>
      </Tooltip>
    </div>
  );
}
