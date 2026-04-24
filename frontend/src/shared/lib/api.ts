// Single RTK Query base API. Feature modules use injectEndpoints to attach
// their own endpoints — keeps middleware registration to one line in the
// store and the dependency graph linear.

import { createApi } from '@reduxjs/toolkit/query/react';
import { baseQueryWithReauth } from './baseQuery';

export const api = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Tasks', 'Task', 'Tags'] as const,
  endpoints: () => ({}),
});
