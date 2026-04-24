import { api } from '@/shared/lib/api';
import type { AuthSession } from './types';
import { tokenStorage } from '@/shared/lib/tokenStorage';
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
        } catch {
          dispatch(loggedOut());
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
