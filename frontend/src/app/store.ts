// Single Redux store. The shared `api` slice carries every feature's RTK
// Query endpoints (injected via createApi().injectEndpoints), so middleware
// registration stays to a single line. Only the auth slice holds client-only
// state today; server data lives in the RTK Query cache.

import { configureStore } from '@reduxjs/toolkit';
import { setupListeners } from '@reduxjs/toolkit/query';

import { api } from '@/shared/lib/api';
import { authReducer } from '@/features/auth/slice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    [api.reducerPath]: api.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(api.middleware),
  devTools: import.meta.env.DEV,
});

setupListeners(store.dispatch);

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
