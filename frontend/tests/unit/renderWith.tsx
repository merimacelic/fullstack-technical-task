// Test harness that wraps components in Redux + Router + Tooltip providers.
// Every component test that touches Redux, hooks like useSearchParams, or
// RTK Query should render through this helper.

import { type ReactNode } from 'react';
import { configureStore } from '@reduxjs/toolkit';
import { setupListeners } from '@reduxjs/toolkit/query';
import { Provider } from 'react-redux';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { render, type RenderOptions, type RenderResult } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Toaster } from 'sonner';
import { TooltipProvider } from '@/shared/ui/tooltip';

import { api } from '@/shared/lib/api';
import { authReducer } from '@/features/auth/slice';

export function makeStore() {
  const store = configureStore({
    reducer: {
      auth: authReducer,
      [api.reducerPath]: api.reducer,
    },
    middleware: (getDefault) => getDefault().concat(api.middleware),
  });
  setupListeners(store.dispatch);
  return store;
}

interface RenderWithOptions extends Omit<RenderOptions, 'wrapper'> {
  route?: string;
  routePattern?: string;
  store?: ReturnType<typeof makeStore>;
}

export function renderWith(
  ui: ReactNode,
  { route = '/', routePattern = '/', store = makeStore(), ...options }: RenderWithOptions = {},
): RenderResult & { store: ReturnType<typeof makeStore>; user: ReturnType<typeof userEvent.setup> } {
  const user = userEvent.setup();
  const result = render(
    <Provider store={store}>
      <TooltipProvider delayDuration={0}>
        <MemoryRouter initialEntries={[route]}>
          <Routes>
            <Route path={routePattern} element={ui} />
          </Routes>
        </MemoryRouter>
        <Toaster />
      </TooltipProvider>
    </Provider>,
    options,
  );
  return { ...result, store, user };
}
