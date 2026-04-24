import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/shared/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/select';
import { useTaskFilters } from '../hooks/useTaskFilters';

interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export function Pagination({
  page,
  pageSize,
  totalCount,
  totalPages,
  hasPreviousPage,
  hasNextPage,
}: PaginationProps) {
  const { setFilter, pageSizeOptions } = useTaskFilters();
  if (totalCount === 0) return null;

  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);

  return (
    <nav
      aria-label="Task list pagination"
      className="flex flex-wrap items-center justify-between gap-3 border-t pt-3"
    >
      <p className="text-xs text-muted-foreground" aria-live="polite">
        Showing <strong>{start}</strong>–<strong>{end}</strong> of <strong>{totalCount}</strong>
      </p>
      <div className="flex items-center gap-2">
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <span className="sr-only sm:not-sr-only">Items per page</span>
          <Select
            value={String(pageSize)}
            onValueChange={(v) => setFilter('pageSize', Number(v))}
          >
            <SelectTrigger className="h-8 w-[72px]" aria-label="Items per page">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {pageSizeOptions.map((opt) => (
                <SelectItem key={opt} value={String(opt)}>
                  {opt}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <Button
          variant="outline"
          size="sm"
          disabled={!hasPreviousPage}
          onClick={() => setFilter('page', page - 1)}
        >
          <ChevronLeft className="h-4 w-4" />
          <span className="sr-only sm:not-sr-only sm:ml-1">Previous</span>
        </Button>
        <span className="text-xs text-muted-foreground">
          Page {page} of {totalPages}
        </span>
        <Button
          variant="outline"
          size="sm"
          disabled={!hasNextPage}
          onClick={() => setFilter('page', page + 1)}
        >
          <span className="sr-only sm:not-sr-only sm:mr-1">Next</span>
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </nav>
  );
}
