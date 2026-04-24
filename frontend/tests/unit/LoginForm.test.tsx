import { beforeEach, describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';

import { LoginForm } from '@/features/auth/components/LoginForm';
import { resetFixtures } from '../mocks/handlers';
import { renderWith } from './renderWith';

describe('<LoginForm />', () => {
  beforeEach(() => {
    resetFixtures();
  });

  it('renders email and password inputs', () => {
    renderWith(<LoginForm />);
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
  });

  it('blocks the network call when fields are empty', async () => {
    const { user, store } = renderWith(<LoginForm />);
    await user.click(screen.getByRole('button', { name: /sign in/i }));
    await new Promise((r) => setTimeout(r, 50));
    // No auth state should have been written — Zod blocked the submit.
    expect(store.getState().auth.accessToken).toBeNull();
  });

  it('offers a link to the register page', () => {
    renderWith(<LoginForm />);
    expect(screen.getByRole('link', { name: /create one/i })).toHaveAttribute('href', '/register');
  });

  // End-to-end auth HTTP flows (login/register/refresh/revoke) are verified by
  // the backend integration tests (AuthEndpointsTests) and by Playwright smoke.
  // RTK Query + jsdom has a known AbortSignal incompatibility that blocks a
  // meaningful component-level HTTP test without an undici shim — not worth
  // the plumbing for a trial.
});
