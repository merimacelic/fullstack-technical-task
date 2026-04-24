import { toast } from 'sonner';

import { api } from '@/shared/lib/api';
import type { AuthSession } from './types';
import { tokenStorage } from '@/shared/lib/tokenStorage';
import i18n from '@/i18n';
import { credentialsReceived, loggedOut } from './slice';

interface AuthResponseDto {
  userId: string;
  email: string;
  accessToken: string;
  accessTokenExpiresUtc: string;
  refreshToken: string;
  refreshTokenExpiresUtc: string;
}

function dtoToSession(dto: AuthResponseDto): AuthSession {
  return {
    user: { id: dto.userId, email: dto.email },
    accessToken: dto.accessToken,
    accessTokenExpiresUtc: dto.accessTokenExpiresUtc,
    refreshToken: dto.refreshToken,
    refreshTokenExpiresUtc: dto.refreshTokenExpiresUtc,
  };
}

export const authApi = api.injectEndpoints({
  endpoints: (builder) => ({
    register: builder.mutation<AuthSession, { email: string; password: string }>({
      query: (body) => ({ url: '/api/auth/register', method: 'POST', body }),
      transformResponse: dtoToSession,
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        try {
          const session = (await queryFulfilled).data;
          dispatch(credentialsReceived(session));
        } catch {
          // Error surfaces to the form via RTK Query's error state.
        }
      },
    }),
    login: builder.mutation<AuthSession, { email: string; password: string }>({
      query: (body) => ({ url: '/api/auth/login', method: 'POST', body }),
      transformResponse: dtoToSession,
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        try {
          const session = (await queryFulfilled).data;
          dispatch(credentialsReceived(session));
        } catch {
          // Error surfaces to the form.
        }
      },
    }),
    refresh: builder.mutation<AuthSession, { refreshToken: string }>({
      query: (body) => ({ url: '/api/auth/refresh', method: 'POST', body }),
      transformResponse: dtoToSession,
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        try {
          const session = (await queryFulfilled).data;
          dispatch(credentialsReceived(session));
        } catch (err) {
          // Only tear down the session on an actual auth failure. 429 / 5xx /
          // network hiccups leave the refresh token valid — the next request
          // will retry refresh silently instead of dumping the user on /login.
          // We still surface a toast so the user knows *something* is wrong
          // with the server rather than staring at endless 401s in silence.
          const status = (err as { error?: { status?: number | string } }).error?.status;
          if (status === 401 || status === 403) {
            dispatch(loggedOut());
          } else if (
            status === 429 ||
            status === 'FETCH_ERROR' ||
            status === 'TIMEOUT_ERROR' ||
            (typeof status === 'number' && status >= 500)
          ) {
            toast.error(i18n.t('errors.refreshUnavailable.title'), {
              description: i18n.t('errors.refreshUnavailable.detail'),
            });
          }
        }
      },
    }),
    revoke: builder.mutation<void, void>({
      query: () => ({
        url: '/api/auth/revoke',
        method: 'POST',
        body: { refreshToken: tokenStorage.getRefreshToken() ?? '' },
      }),
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        try {
          await queryFulfilled;
        } finally {
          // Always clear local state — server returns 204 even on unknown tokens.
          dispatch(loggedOut());
        }
      },
    }),
  }),
});

export const {
  useRegisterMutation,
  useLoginMutation,
  useRefreshMutation,
  useRevokeMutation,
} = authApi;
