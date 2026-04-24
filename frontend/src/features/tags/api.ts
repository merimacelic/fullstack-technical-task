import { api } from '@/shared/lib/api';
import type { TagDto } from './types';

export const tagsApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getTags: builder.query<TagDto[], void>({
      query: () => '/api/tags',
      providesTags: (result) =>
        result
          ? [
              ...result.map((t) => ({ type: 'Tags' as const, id: t.id })),
              { type: 'Tags' as const, id: 'LIST' },
            ]
          : [{ type: 'Tags' as const, id: 'LIST' }],
    }),
    createTag: builder.mutation<TagDto, { name: string }>({
      query: (body) => ({ url: '/api/tags', method: 'POST', body }),
      invalidatesTags: [{ type: 'Tags', id: 'LIST' }],
    }),
    renameTag: builder.mutation<TagDto, { id: string; name: string }>({
      query: ({ id, name }) => ({ url: `/api/tags/${id}`, method: 'PUT', body: { name } }),
      invalidatesTags: (_r, _e, arg) => [
        { type: 'Tags', id: arg.id },
        { type: 'Tags', id: 'LIST' },
      ],
    }),
    deleteTag: builder.mutation<void, string>({
      query: (id) => ({ url: `/api/tags/${id}`, method: 'DELETE' }),
      invalidatesTags: (_r, _e, id) => [
        { type: 'Tags', id },
        { type: 'Tags', id: 'LIST' },
        // Deleting a tag removes it from all tasks — refresh the task list too.
        { type: 'Tasks', id: 'LIST' },
      ],
    }),
  }),
});

export const {
  useGetTagsQuery,
  useCreateTagMutation,
  useRenameTagMutation,
  useDeleteTagMutation,
} = tagsApi;
