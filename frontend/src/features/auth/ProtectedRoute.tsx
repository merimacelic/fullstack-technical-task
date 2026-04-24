// Gate every authenticated route. Three states:
//   1. We have an access token → render children.
//   2. We don't, but a refresh token exists in localStorage → attempt a refresh
//      on mount; render skeleton meanwhile; on failure fall through to /login.
//   3. No token at all → redirect to /login carrying the current location so
//      we can navigate back after a successful sign-in.

import { useEffect, useRef, type ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';

import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { Skeleton } from '@/shared/ui/skeleton';
import { tokenStorage } from '@/shared/lib/tokenStorage';
import { bootstrapFinished, selectIsAuthenticated, selectIsBootstrapping } from './slice';
import { useRefreshMutation } from './api';

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const location = useLocation();
  const dispatch = useAppDispatch();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const isBootstrapping = useAppSelector(selectIsBootstrapping);
  const [refresh] = useRefreshMutation();

  // StrictMode runs this effect twice in Dev. Without the ref guard both
  // passes POST /api/auth/refresh with the same old token — the second one
  // trips the backend's refresh-token-reuse detector and revokes the whole
  // family, killing the session we'd just bootstrapped.
  const attempted = useRef(false);

  useEffect(() => {
    if (isAuthenticated || !isBootstrapping || attempted.current) return;
    const token = tokenStorage.getRefreshToken();
    if (!token) {
      dispatch(bootstrapFinished());
      return;
    }
    attempted.current = true;
    refresh({ refreshToken: token })
      .unwrap()
      .catch(() => {
        // refresh api dispatches loggedOut on failure; this just catches the
        // promise so it doesn't bubble as an unhandled rejection.
      });
  }, [isAuthenticated, isBootstrapping, refresh, dispatch]);

  if (isBootstrapping) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4 p-6">
        <Skeleton className="h-10 w-64" />
        <Skeleton className="h-4 w-48" />
        <span className="sr-only">Restoring your session…</span>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <>{children}</>;
}
