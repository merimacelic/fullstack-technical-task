import { type ReactNode } from 'react';
import { Provider } from 'react-redux';
import { Toaster } from 'sonner';

import { ThemeProvider } from '@/shared/layout/ThemeProvider';
import { useAuthStorageSync } from '@/features/auth/useAuthStorageSync';
import { store } from './store';

// Tiny component so the hook can use Redux — `Provider` must wrap it.
function AuthStorageSync() {
  useAuthStorageSync();
  return null;
}

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <Provider store={store}>
      <ThemeProvider>
        <AuthStorageSync />
        {children}
        <Toaster position="top-right" richColors closeButton />
      </ThemeProvider>
    </Provider>
  );
}
