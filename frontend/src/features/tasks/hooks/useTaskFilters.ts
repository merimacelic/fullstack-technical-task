// Filter state lives in the URL so bookmarks + shared links are always
// reproducible. This hook is the single reader/writer seam.

import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';

import {
  DEFAULT_FILTERS,
  DEFAULT_VIEW_MODE,
  TASK_PRIORITIES,
  TASK_SORT_FIELDS,
  TASK_STATUSES,
  TASK_VIEW_MODES,
  type TaskFilters,
  type TaskPriority,
  type TaskSortBy,
  type TaskStatus,
  type TaskViewMode,
} from '../types';

const PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;
const MIN_PAGE_SIZE = 1;
const MAX_PAGE_SIZE = 100;

function asStatuses(values: string[]): TaskStatus[] | undefined {
  const valid = values.filter((v): v is TaskStatus =>
    (TASK_STATUSES as readonly string[]).includes(v),
  );
  return valid.length > 0 ? valid : undefined;
}

function asPriorities(values: string[]): TaskPriority[] | undefined {
  const valid = values.filter((v): v is TaskPriority =>
    (TASK_PRIORITIES as readonly string[]).includes(v),
  );
  return valid.length > 0 ? valid : undefined;
}

function asTagIds(values: string[]): string[] | undefined {
  return values.length > 0 ? values : undefined;
}

function asSortBy(v: string | null): TaskSortBy {
  return v && (TASK_SORT_FIELDS as readonly string[]).includes(v)
    ? (v as TaskSortBy)
    : DEFAULT_FILTERS.sortBy;
}

function asDirection(v: string | null): TaskFilters['sortDirection'] {
  return v === 'Ascending' ? 'Ascending' : DEFAULT_FILTERS.sortDirection;
}

function asViewMode(v: string | null): TaskViewMode {
  return v && (TASK_VIEW_MODES as readonly string[]).includes(v)
    ? (v as TaskViewMode)
    : DEFAULT_VIEW_MODE;
}

function clampPage(n: number): number {
  return Number.isFinite(n) && n >= 1 ? Math.floor(n) : 1;
}

function clampPageSize(n: number): number {
  if (!Number.isFinite(n)) return DEFAULT_FILTERS.pageSize;
  return Math.max(MIN_PAGE_SIZE, Math.min(MAX_PAGE_SIZE, Math.floor(n)));
}

export function useTaskFilters(): {
  filters: TaskFilters;
  setFilter: <K extends keyof TaskFilters>(key: K, value: TaskFilters[K] | undefined) => void;
  resetFilters: () => void;
  pageSizeOptions: readonly number[];
  viewMode: TaskViewMode;
  setViewMode: (mode: TaskViewMode) => void;
} {
  const [searchParams, setSearchParams] = useSearchParams();

  const filters = useMemo<TaskFilters>(
    () => ({
      statuses: asStatuses(searchParams.getAll('statuses')),
      priorities: asPriorities(searchParams.getAll('priorities')),
      search: searchParams.get('search') ?? undefined,
      tagIds: asTagIds(searchParams.getAll('tagIds')),
      sortBy: asSortBy(searchParams.get('sortBy')),
      sortDirection: asDirection(searchParams.get('sortDirection')),
      page: clampPage(Number(searchParams.get('page') ?? '1')),
      pageSize: clampPageSize(Number(searchParams.get('pageSize') ?? DEFAULT_FILTERS.pageSize)),
    }),
    [searchParams],
  );

  const viewMode = useMemo(() => asViewMode(searchParams.get('view')), [searchParams]);

  const setFilter = useCallback(
    <K extends keyof TaskFilters>(key: K, value: TaskFilters[K] | undefined) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          next.delete(key);
          if (Array.isArray(value)) {
            // Empty arrays encode as "no filter" — skip writing any param.
            value.forEach((v) => next.append(key, String(v)));
          } else if (value !== undefined && value !== null && value !== '') {
            next.set(key, String(value));
          }
          // Any filter change (except pagination) resets to page 1.
          if (key !== 'page') next.delete('page');
          return next;
        },
        { replace: true },
      );
    },
    [setSearchParams],
  );

  const setViewMode = useCallback(
    (mode: TaskViewMode) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          if (mode === DEFAULT_VIEW_MODE) {
            next.delete('view');
          } else {
            next.set('view', mode);
          }
          return next;
        },
        { replace: true },
      );
    },
    [setSearchParams],
  );

  const resetFilters = useCallback(() => {
    setSearchParams(
      (prev) => {
        // Preserve view preference when clearing filters.
        const next = new URLSearchParams();
        const view = prev.get('view');
        if (view) next.set('view', view);
        return next;
      },
      { replace: true },
    );
  }, [setSearchParams]);

  return {
    filters,
    setFilter,
    resetFilters,
    pageSizeOptions: PAGE_SIZE_OPTIONS,
    viewMode,
    setViewMode,
  };
}
