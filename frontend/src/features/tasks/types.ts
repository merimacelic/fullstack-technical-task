// Mirrors TaskManagement.Application.Tasks.Responses.TaskResponse.
// Enum string casing matches the backend's smart-enum .Name exactly
// (case-sensitive — Pending / InProgress / Completed).

export type TaskStatus = 'Pending' | 'InProgress' | 'Completed';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export type TaskSortBy =
  | 'CreatedAt'
  | 'UpdatedAt'
  | 'DueDate'
  | 'Priority'
  | 'Title'
  | 'Order';

export type SortDirection = 'Ascending' | 'Descending';

export const TASK_STATUSES: readonly TaskStatus[] = ['Pending', 'InProgress', 'Completed'];
export const TASK_PRIORITIES: readonly TaskPriority[] = ['Low', 'Medium', 'High', 'Critical'];
export const TASK_SORT_FIELDS: readonly TaskSortBy[] = [
  'CreatedAt',
  'UpdatedAt',
  'DueDate',
  'Priority',
  'Title',
  'Order',
];

export interface TaskDto {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDateUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  completedAtUtc: string | null;
  orderKey: number;
  tagIds: string[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface TaskFilters {
  status?: TaskStatus;
  priority?: TaskPriority;
  search?: string;
  tagId?: string;
  sortBy: TaskSortBy;
  sortDirection: SortDirection;
  page: number;
  pageSize: number;
}

export const DEFAULT_FILTERS: TaskFilters = {
  sortBy: 'CreatedAt',
  sortDirection: 'Descending',
  page: 1,
  pageSize: 20,
};
