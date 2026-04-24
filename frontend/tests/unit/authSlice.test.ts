import { beforeEach, describe, expect, it } from 'vitest';
import {
  authReducer,
  bootstrapFinished,
  credentialsReceived,
  loggedOut,
  type AuthState,
} from '@/features/auth/slice';
import { tokenStorage } from '@/shared/lib/tokenStorage';

const INITIAL: AuthState = {
  user: null,
  accessToken: null,
  accessTokenExpiresUtc: null,
  refreshTokenExpiresUtc: null,
  isBootstrapping: false,
};

describe('authSlice', () => {
  beforeEach(() => {
    tokenStorage.clearRefreshToken();
  });

  it('stores credentials on credentialsReceived and persists refresh token', () => {
    const next = authReducer(
      INITIAL,
      credentialsReceived({
        user: { id: 'u1', email: 'a@b.c' },
        accessToken: 'access',
        accessTokenExpiresUtc: '2099-01-01T00:00:00Z',
        refreshToken: 'refresh',
        refreshTokenExpiresUtc: '2099-02-01T00:00:00Z',
      }),
    );

    expect(next.user?.email).toBe('a@b.c');
    expect(next.accessToken).toBe('access');
    expect(next.isBootstrapping).toBe(false);
    expect(tokenStorage.getRefreshToken()).toBe('refresh');
  });

  it('clears everything on loggedOut', () => {
    tokenStorage.setRefreshToken('refresh');
    const state: AuthState = {
      user: { id: 'u1', email: 'a@b.c' },
      accessToken: 'access',
      accessTokenExpiresUtc: '2099-01-01T00:00:00Z',
      refreshTokenExpiresUtc: '2099-02-01T00:00:00Z',
      isBootstrapping: false,
    };
    const next = authReducer(state, loggedOut());
    expect(next.accessToken).toBeNull();
    expect(next.user).toBeNull();
    expect(tokenStorage.getRefreshToken()).toBeNull();
  });

  it('clears isBootstrapping on bootstrapFinished', () => {
    const state: AuthState = { ...INITIAL, isBootstrapping: true };
    expect(authReducer(state, bootstrapFinished()).isBootstrapping).toBe(false);
  });
});
