// Cross-tab session sync. When another tab clears the refresh token
// (logout) or replaces it with a different one (login as a different
// user), mirror the change in this tab so both windows agree on the
// current session instead of drifting until the next 401.

import { useEffect } from 'react';

import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { REFRESH_TOKEN_KEY } from '@/shared/lib/tokenStorage';
import { loggedOut, selectIsAuthenticated } from './slice';

export function useAuthStorageSync(): void {
  const dispatch = useAppDispatch();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  useEffect(() => {
    function onStorage(event: StorageEvent) {
      // `storage` events only fire in OTHER tabs — this tab never sees its
      // own writes — so any event here is a cross-tab signal.
      if (event.key !== REFRESH_TOKEN_KEY) return;

      // Token cleared or replaced with a different value → log this tab out.
      // If it's a fresh login the user can authenticate again in this tab;
      // silently adopting the new token would leak one user's session into
      // another's window.
      const stillAuthenticated =
        event.newValue !== null && event.newValue !== '' && event.newValue === event.oldValue;
      if (!stillAuthenticated && isAuthenticated) {
        dispatch(loggedOut());
      }
    }

    window.addEventListener('storage', onStorage);
    return () => window.removeEventListener('storage', onStorage);
  }, [dispatch, isAuthenticated]);
}
