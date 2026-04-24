// Auth slice — the only Redux state that holds credentials. Access token
// lives in memory (never persisted); refresh token is mirrored to localStorage
// via tokenStorage so a new access token can be minted on reload. See ADR 0008.

import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

import { tokenStorage } from '@/shared/lib/tokenStorage';
import type { AuthSession, AuthUser } from './types';

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  accessTokenExpiresUtc: string | null;
  refreshTokenExpiresUtc: string | null;
  isBootstrapping: boolean;
}

const initialState: AuthState = {
  user: null,
  accessToken: null,
  accessTokenExpiresUtc: null,
  refreshTokenExpiresUtc: null,
  isBootstrapping: Boolean(tokenStorage.getRefreshToken()),
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    credentialsReceived(state, action: PayloadAction<AuthSession>) {
      state.user = action.payload.user;
      state.accessToken = action.payload.accessToken;
      state.accessTokenExpiresUtc = action.payload.accessTokenExpiresUtc;
      state.refreshTokenExpiresUtc = action.payload.refreshTokenExpiresUtc;
      state.isBootstrapping = false;
      tokenStorage.setRefreshToken(action.payload.refreshToken);
    },
    bootstrapFinished(state) {
      state.isBootstrapping = false;
    },
    loggedOut(state) {
      state.user = null;
      state.accessToken = null;
      state.accessTokenExpiresUtc = null;
      state.refreshTokenExpiresUtc = null;
      state.isBootstrapping = false;
      tokenStorage.clearRefreshToken();
    },
  },
});

export const { credentialsReceived, bootstrapFinished, loggedOut } = authSlice.actions;
export const authReducer = authSlice.reducer;
export type { AuthState };

// Selectors
import type { RootState } from '@/app/store';

export const selectAuth = (state: RootState) => state.auth;
export const selectCurrentUser = (state: RootState) => state.auth.user;
export const selectIsAuthenticated = (state: RootState) => Boolean(state.auth.accessToken);
export const selectIsBootstrapping = (state: RootState) => state.auth.isBootstrapping;
