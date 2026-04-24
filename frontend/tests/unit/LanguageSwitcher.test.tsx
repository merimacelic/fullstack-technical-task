import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';

import i18n, { LOCALE_STORAGE_KEY } from '@/i18n';
import { LanguageSwitcher } from '@/shared/layout/LanguageSwitcher';
import { renderWith } from './renderWith';

// Exercises the header flag-icon dropdown: the menu lists both locales,
// picking one updates the active language, and the trigger's aria-label
// reflects the localised "Change language" copy for screen readers.

describe('<LanguageSwitcher />', () => {
  beforeEach(async () => {
    window.localStorage.clear();
    await i18n.changeLanguage('en');
  });

  afterEach(async () => {
    window.localStorage.clear();
    await i18n.changeLanguage('en');
  });

  it('renders a button labelled for screen readers', () => {
    renderWith(<LanguageSwitcher />);
    expect(screen.getByRole('button', { name: /change language/i })).toBeInTheDocument();
  });

  it('opens a menu listing both supported locales', async () => {
    const { user } = renderWith(<LanguageSwitcher />);

    await user.click(screen.getByRole('button', { name: /change language/i }));

    expect(await screen.findByRole('menuitem', { name: /english/i })).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: /malti/i })).toBeInTheDocument();
  });

  it('switches the app locale when a menu item is chosen', async () => {
    const { user } = renderWith(<LanguageSwitcher />);

    await user.click(screen.getByRole('button', { name: /change language/i }));
    await user.click(await screen.findByRole('menuitem', { name: /malti/i }));

    await waitFor(() => expect(i18n.language).toBe('mt'));
    expect(window.localStorage.getItem(LOCALE_STORAGE_KEY)).toBe('mt');
    // The trigger's aria-label is driven by the Maltese "Ibdel il-lingwa" key.
    expect(screen.getByRole('button', { name: /ibdel il-lingwa/i })).toBeInTheDocument();
  });
});
