import { api } from '@/shared/lib/api';
import type { PagedResult, TaskDto, TaskFilters } from './types';

export interface CreateTaskPayload {
  title: string;
  description?: string | null;
  priority: string;
  dueDateUtc?: string | null;
  tagIds?: string[];
  status?: string;
}

export interface UpdateTaskPayload {
  id: string;
  title: string;
  description?: string | null;
  priority: string;
  dueDateUtc?: string | null;
  tagIds?: string[] | null;
  status?: string;
}

function buildQueryParams(filters: TaskFilters): URLSearchParams {
  const params = new URLSearchParams();
  filters.statuses?.forEach((s) => params.append('Statuses', s));
  filters.priorities?.forEach((p) => params.append('Priorities', p));
  filters.tagIds?.forEach((id) => params.append('TagIds', id));
  if (filters.search) params.set('Search', filters.search);
  params.set('SortBy', filters.sortBy);
  params.set('SortDirection', filters.sortDirection);
  params.set('Page', String(filters.page));
  params.set('PageSize', String(filters.pageSize));
  return params;
}

export const tasksApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getTasks: builder.query<PagedResult<TaskDto>, TaskFilters>({
      query: (filters) => `/api/tasks?${buildQueryParams(filters).toString()}`,
      providesTags: (result) =>
        result
          ? [
              ...result.items.map((t) => ({ type: 'Task' as const, id: t.id })),
              { type: 'Tasks' as const, id: 'LIST' },
            ]
          : [{ type: 'Tasks' as const, id: 'LIST' }],
    }),
    getTaskById: builder.query<TaskDto, string>({
      query: (id) => `/api/tasks/${id}`,
      providesTags: (_r, _e, id) => [{ type: 'Task', id }],
    }),
    createTask: builder.mutation<TaskDto, CreateTaskPayload>({
      query: (body) => ({ url: '/api/tasks', method: 'POST', body }),
      invalidatesTags: [{ type: 'Tasks', id: 'LIST' }],
    }),
    updateTask: builder.mutation<TaskDto, UpdateTaskPayload>({
      query: ({ id, ...body }) => ({
        url: `/api/tasks/${id}`,
        method: 'PUT',
        body,
      }),
      invalidatesTags: (_r, _e, arg) => [
        { type: 'Task', id: arg.id },
        { type: 'Tasks', id: 'LIST' },
      ],
    }),
    deleteTask: builder.mutation<void, string>({
      query: (id) => ({ url: `/api/tasks/${id}`, method: 'DELETE' }),
      invalidatesTags: (_r, _e, id) => [
        { type: 'Task', id },
        { type: 'Tasks', id: 'LIST' },
      ],
    }),
    completeTask: builder.mutation<TaskDto, string>({
      query: (id) => ({ url: `/api/tasks/${id}/complete`, method: 'PATCH' }),
      invalidatesTags: (_r, _e, id) => [
        { type: 'Task', id },
        { type: 'Tasks', id: 'LIST' },
      ],
    }),
    reopenTask: builder.mutation<TaskDto, string>({
      query: (id) => ({ url: `/api/tasks/${id}/reopen`, method: 'PATCH' }),
      invalidatesTags: (_r, _e, id) => [
        { type: 'Task', id },
        { type: 'Tasks', id: 'LIST' },
      ],
    }),
    changeTaskStatus: builder.mutation<TaskDto, { id: string; status: string }>({
      query: ({ id, status }) => ({
        url: `/api/tasks/${id}/status`,
        method: 'PATCH',
        body: { status },
      }),
      invalidatesTags: (_r, _e, arg) => [
        { type: 'Task', id: arg.id },
        { type: 'Tasks', id: 'LIST' },
      ],
    }),
    changeTaskPriority: builder.mutation<TaskDto, { id: string; priority: string }>({
      query: ({ id, priority }) => ({
        url: `/api/tasks/${id}/priority`,
        method: 'PATCH',
        body: { priority },
      }),
      invalidatesTags: (_r, _e, arg) => [
        { type: 'Task', id: arg.id },
        { type: 'Tasks', id: 'LIST' },
      ],
    }),
    reorderTask: builder.mutation<
      TaskDto,
      {
        id: string;
        // Visual neighbours after the move: the tasks immediately above / below
        // the dropped card in the user's current view. The optimistic patch
        // uses these directly (they index into the visually-ordered list).
        visualPreviousId: string | null;
        visualNextId: string | null;
        filters: TaskFilters;
      }
    >({
      query: ({ id, visualPreviousId, visualNextId, filters }) => {
        // Backend contract (see OrderKeyService.BetweenAsync): previousTaskId
        // must have a *lower* OrderKey than nextTaskId. In a descending view
        // the visually-above task has the *higher* key, so swap to preserve
        // the ascending-key semantics the service is built on.
        const descending =
          filters.sortBy === 'Order' && filters.sortDirection === 'Descending';
        const previousTaskId = descending ? visualNextId : visualPreviousId;
        const nextTaskId = descending ? visualPreviousId : visualNextId;
        return {
          url: `/api/tasks/${id}/reorder`,
          method: 'PATCH',
          body: { previousTaskId, nextTaskId },
        };
      },
      // Optimistic list patch: move the item locally so the user sees the
      // result instantly. The server returns the authoritative orderKey; we
      // invalidate on success to pull the rebalanced values if needed.
      async onQueryStarted(
        { id, visualPreviousId, visualNextId, filters },
        { dispatch, queryFulfilled },
      ) {
        const patch = dispatch(
          tasksApi.util.updateQueryData('getTasks', filters, (draft) => {
            const movingIdx = draft.items.findIndex((t) => t.id === id);
            if (movingIdx < 0) return;
            const moving = draft.items.splice(movingIdx, 1)[0];
            if (!moving) return;

            let insertAt: number;
            if (visualPreviousId) {
              const prevIdx = draft.items.findIndex((t) => t.id === visualPreviousId);
              insertAt = prevIdx >= 0 ? prevIdx + 1 : 0;
            } else if (visualNextId) {
              const nextIdx = draft.items.findIndex((t) => t.id === visualNextId);
              insertAt = nextIdx >= 0 ? nextIdx : draft.items.length;
            } else {
              insertAt = 0;
            }
            draft.items.splice(insertAt, 0, moving);
          }),
        );
        try {
          await queryFulfilled;
        } catch {
          patch.undo();
        }
      },
      invalidatesTags: [{ type: 'Tasks', id: 'LIST' }],
    }),
  }),
});

export const {
  useGetTasksQuery,
  useLazyGetTasksQuery,
  useGetTaskByIdQuery,
  useCreateTaskMutation,
  useUpdateTaskMutation,
  useDeleteTaskMutation,
  useCompleteTaskMutation,
  useReopenTaskMutation,
  useChangeTaskStatusMutation,
  useChangeTaskPriorityMutation,
  useReorderTaskMutation,
} = tasksApi;
