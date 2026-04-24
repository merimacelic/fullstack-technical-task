// Filter state lives in the URL so bookmarks + shared links are always
// reproducible. This hook is the single reader/writer seam.

import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';

import {
  DEFAULT_FILTERS,
  TASK_PRIORITIES,
  TASK_SORT_FIELDS,
  TASK_STATUSES,
  type TaskFilters,
  type TaskPriority,
  type TaskSortBy,
  type TaskStatus,
} from '../types';

const PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;
const MIN_PAGE_SIZE = 1;
const MAX_PAGE_SIZE = 100;

function asStatus(v: string | null): TaskStatus | undefined {
  return v && (TASK_STATUSES as readonly string[]).includes(v) ? (v as TaskStatus) : undefined;
}

function asPriority(v: string | null): TaskPriority | undefined {
  return v && (TASK_PRIORITIES as readonly string[]).includes(v) ? (v as TaskPriority) : undefined;
}

function asSortBy(v: string | null): TaskSortBy {
  return v && (TASK_SORT_FIELDS as readonly string[]).includes(v)
    ? (v as TaskSortBy)
    : DEFAULT_FILTERS.sortBy;
}

function asDirection(v: string | null): TaskFilters['sortDirection'] {
  return v === 'Ascending' ? 'Ascending' : DEFAULT_FILTERS.sortDirection;
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
} {
  const [searchParams, setSearchParams] = useSearchParams();

  const filters = useMemo<TaskFilters>(
    () => ({
      status: asStatus(searchParams.get('status')),
      priority: asPriority(searchParams.get('priority')),
      search: searchParams.get('search') ?? undefined,
      tagId: searchParams.get('tagId') ?? undefined,
      sortBy: asSortBy(searchParams.get('sortBy')),
      sortDirection: asDirection(searchParams.get('sortDirection')),
      page: clampPage(Number(searchParams.get('page') ?? '1')),
      pageSize: clampPageSize(Number(searchParams.get('pageSize') ?? DEFAULT_FILTERS.pageSize)),
    }),
    [searchParams],
  );

  const setFilter = useCallback(
    <K extends keyof TaskFilters>(key: K, value: TaskFilters[K] | undefined) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          if (value === undefined || value === null || value === '') {
            next.delete(key);
          } else {
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

  const resetFilters = useCallback(() => {
    setSearchParams(new URLSearchParams(), { replace: true });
  }, [setSearchParams]);

  return { filters, setFilter, resetFilters, pageSizeOptions: PAGE_SIZE_OPTIONS };
}
