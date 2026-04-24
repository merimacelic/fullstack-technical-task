import { useEffect, useState } from 'react';
import { Search, SlidersHorizontal, X } from 'lucide-react';

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
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { useGetTagsQuery } from '@/features/tags/api';
import { useTaskFilters } from '../hooks/useTaskFilters';
import { TASK_PRIORITIES, TASK_STATUSES, type TaskPriority, type TaskStatus } from '../types';

const ALL_VALUE = '__all__';

const SORT_LABELS: Record<string, string> = {
  CreatedAt: 'Created',
  UpdatedAt: 'Updated',
  DueDate: 'Due date',
  Priority: 'Priority',
  Title: 'Title',
  Order: 'Manual order',
};

export function TaskFilters() {
  const { filters, setFilter, resetFilters } = useTaskFilters();
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

  // Keep the local draft in sync when the user clears via resetFilters.
  useEffect(() => {
    setSearchDraft(filters.search ?? '');
  }, [filters.search]);

  const hasActiveFilters = Boolean(
    filters.status ?? filters.priority ?? filters.search ?? filters.tagId,
  );

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-2">
        <div className="relative flex-1 min-w-[180px]">
          <Search
            className="pointer-events-none absolute left-2 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
            aria-hidden
          />
          <Input
            type="search"
            placeholder="Search title and description…"
            value={searchDraft}
            onChange={(e) => setSearchDraft(e.target.value)}
            aria-label="Search tasks"
            className="pl-8"
          />
        </div>

        {/* Inline filter bar on md+ screens */}
        <div className="hidden items-center gap-2 md:flex">
          <StatusSelect value={filters.status} onChange={(v) => setFilter('status', v)} />
          <PrioritySelect value={filters.priority} onChange={(v) => setFilter('priority', v)} />
          <TagSelect
            value={filters.tagId}
            onChange={(v) => setFilter('tagId', v)}
            tags={tags ?? []}
          />
          <Separator />
          <SortControls />
        </div>

        {/* Collapsed panel on smaller screens */}
        <Sheet>
          <SheetTrigger asChild>
            <Button variant="outline" size="sm" className="md:hidden" aria-label="Open filters">
              <SlidersHorizontal className="mr-2 h-4 w-4" />
              Filters
            </Button>
          </SheetTrigger>
          <SheetContent side="bottom" className="max-h-[85vh] overflow-y-auto">
            <SheetHeader>
              <SheetTitle>Filters</SheetTitle>
            </SheetHeader>
            <div className="mt-4 flex flex-col gap-4">
              <Field label="Status">
                <StatusSelect value={filters.status} onChange={(v) => setFilter('status', v)} />
              </Field>
              <Field label="Priority">
                <PrioritySelect value={filters.priority} onChange={(v) => setFilter('priority', v)} />
              </Field>
              <Field label="Tag">
                <TagSelect
                  value={filters.tagId}
                  onChange={(v) => setFilter('tagId', v)}
                  tags={tags ?? []}
                />
              </Field>
              <Field label="Sort">
                <SortControls />
              </Field>
            </div>
          </SheetContent>
        </Sheet>

        {hasActiveFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              resetFilters();
              setSearchDraft('');
            }}
          >
            <X className="mr-1 h-4 w-4" />
            Clear
          </Button>
        )}
      </div>
    </div>
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

function Separator() {
  return <span className="h-5 w-px bg-border" aria-hidden />;
}

function StatusSelect({
  value,
  onChange,
}: {
  value: TaskStatus | undefined;
  onChange: (value: TaskStatus | undefined) => void;
}) {
  return (
    <Select
      value={value ?? ALL_VALUE}
      onValueChange={(v) => onChange(v === ALL_VALUE ? undefined : (v as TaskStatus))}
    >
      <SelectTrigger className="w-[150px]" aria-label="Filter by status">
        <SelectValue placeholder="Status" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={ALL_VALUE}>All statuses</SelectItem>
        {TASK_STATUSES.map((s) => (
          <SelectItem key={s} value={s}>
            {s === 'InProgress' ? 'In progress' : s}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

function PrioritySelect({
  value,
  onChange,
}: {
  value: TaskPriority | undefined;
  onChange: (value: TaskPriority | undefined) => void;
}) {
  return (
    <Select
      value={value ?? ALL_VALUE}
      onValueChange={(v) => onChange(v === ALL_VALUE ? undefined : (v as TaskPriority))}
    >
      <SelectTrigger className="w-[150px]" aria-label="Filter by priority">
        <SelectValue placeholder="Priority" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={ALL_VALUE}>All priorities</SelectItem>
        {TASK_PRIORITIES.map((p) => (
          <SelectItem key={p} value={p}>
            {p}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

function TagSelect({
  value,
  onChange,
  tags,
}: {
  value: string | undefined;
  onChange: (value: string | undefined) => void;
  tags: readonly { id: string; name: string }[];
}) {
  return (
    <Select
      value={value ?? ALL_VALUE}
      onValueChange={(v) => onChange(v === ALL_VALUE ? undefined : v)}
    >
      <SelectTrigger className="w-[150px]" aria-label="Filter by tag">
        <SelectValue placeholder="Tag" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={ALL_VALUE}>All tags</SelectItem>
        {tags.map((tag) => (
          <SelectItem key={tag.id} value={tag.id}>
            {tag.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

function SortControls() {
  const { filters, setFilter } = useTaskFilters();
  return (
    <div className="flex items-center gap-2">
      <Select value={filters.sortBy} onValueChange={(v) => setFilter('sortBy', v as never)}>
        <SelectTrigger className="w-[150px]" aria-label="Sort by">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {Object.entries(SORT_LABELS).map(([key, label]) => (
            <SelectItem key={key} value={key}>
              {label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <Button
        variant="outline"
        size="sm"
        aria-label={`Sort direction: ${filters.sortDirection}`}
        onClick={() =>
          setFilter(
            'sortDirection',
            filters.sortDirection === 'Ascending' ? 'Descending' : 'Ascending',
          )
        }
      >
        {filters.sortDirection === 'Ascending' ? 'Asc ↑' : 'Desc ↓'}
      </Button>
    </div>
  );
}
