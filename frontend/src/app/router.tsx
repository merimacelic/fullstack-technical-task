import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom';

import { AppShell } from '@/shared/layout/AppShell';
import { ProtectedRoute } from '@/features/auth/ProtectedRoute';
import { LoginPage } from '@/features/auth/pages/LoginPage';
import { RegisterPage } from '@/features/auth/pages/RegisterPage';
import { TasksPage } from '@/features/tasks/pages/TasksPage';
import { TaskDetailsPage } from '@/features/tasks/pages/TaskDetailsPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { ErrorBoundaryPage } from '@/pages/ErrorBoundaryPage';

const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
    errorElement: <ErrorBoundaryPage />,
  },
  {
    path: '/register',
    element: <RegisterPage />,
    errorElement: <ErrorBoundaryPage />,
  },
  {
    path: '/',
    element: (
      <ProtectedRoute>
        <AppShell />
      </ProtectedRoute>
    ),
    errorElement: <ErrorBoundaryPage />,
    children: [
      { index: true, element: <Navigate to="/tasks" replace /> },
      { path: 'tasks', element: <TasksPage /> },
      { path: 'tasks/:id', element: <TaskDetailsPage /> },
    ],
  },
  { path: '*', element: <NotFoundPage /> },
]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
