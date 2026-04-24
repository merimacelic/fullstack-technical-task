// RTK Query base query with silent refresh-on-401. A module-scoped promise
// acts as a mutex so a burst of concurrent 401s triggers only one refresh
// round-trip. The refresh itself is routed through the RTK Query `refresh`
// mutation so credentialsReceived / loggedOut are dispatched in exactly one
// place (authApi's onQueryStarted) — both this baseQuery and ProtectedRoute's
// bootstrap hit the same code path.

import { fetchBaseQuery } from '@reduxjs/toolkit/query';
import type {
  BaseQueryApi,
  BaseQueryFn,
  FetchArgs,
  FetchBaseQueryError,
  FetchBaseQueryMeta,
} from '@reduxjs/toolkit/query';

import { loggedOut } from '@/features/auth/slice';
import type { RootState } from '@/app/store';
import { config } from './config';
import { newCorrelationId } from './correlationId';
import { tokenStorage } from './tokenStorage';

const rawBaseQuery = fetchBaseQuery({
  baseUrl: config.apiBaseUrl,
  credentials: 'same-origin',
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as RootState).auth.accessToken;
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    if (!headers.has('X-Correlation-Id')) {
      headers.set('X-Correlation-Id', newCorrelationId());
    }
    return headers;
  },
});

let inFlightRefresh: Promise<boolean> | null = null;

async function attemptRefresh(api: BaseQueryApi): Promise<boolean> {
  const refreshToken = tokenStorage.getRefreshToken();
  if (!refreshToken) return false;

  // Dynamic import avoids a module-load cycle: baseQuery → authApi → api →
  // baseQuery. By the time this runs the whole graph is initialised.
  const { authApi } = await import('@/features/auth/api');
  const thunk = api.dispatch(authApi.endpoints.refresh.initiate({ refreshToken }));
  try {
    const result = await thunk;
    return !('error' in result && result.error);
  } finally {
    // Mutation subscriptions are cheap but still best-effort cleaned up so the
    // cache entry doesn't linger past its usefulness.
    thunk.reset?.();
  }
}

export const baseQueryWithReauth: BaseQueryFn<
  string | FetchArgs,
  unknown,
  FetchBaseQueryError,
  object,
  FetchBaseQueryMeta
> = async (args, api, extraOptions) => {
  let result = await rawBaseQuery(args, api, extraOptions);

  // Don't attempt refresh on the auth endpoints themselves — that would loop.
  const url = typeof args === 'string' ? args : args.url;
  const isAuthCall = url.startsWith('/api/auth/');

  if (result.error?.status === 401 && !isAuthCall) {
    try {
      inFlightRefresh ??= attemptRefresh(api);
      const refreshed = await inFlightRefresh;

      if (refreshed) {
        result = await rawBaseQuery(args, api, extraOptions);
        // A 401 after a successful refresh means something is badly wrong
        // (stale claims, clock skew, server-side invalidation). Bail out
        // rather than loop — the user has to log in again.
        if (result.error?.status === 401) {
          api.dispatch(loggedOut());
        }
      }
      // On refresh failure authApi.refresh's onQueryStarted has already
      // dispatched loggedOut, so the caller sees the original 401 and
      // ProtectedRoute redirects to /login.
    } finally {
      inFlightRefresh = null;
    }
  }

  return result;
};
