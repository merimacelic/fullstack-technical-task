import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import i18n, { LOCALE_STORAGE_KEY } from '@/i18n';
import { useLocale } from '@/i18n/useLocale';

// Covers the locale-switching contract: setLocale persists to localStorage,
// flips i18next's active language (so every useTranslation() subscriber
// re-renders), and keeps the <html lang> attribute in sync for screen readers.

describe('useLocale', () => {
  beforeEach(async () => {
    window.localStorage.clear();
    await i18n.changeLanguage('en');
  });

  afterEach(async () => {
    window.localStorage.clear();
    await i18n.changeLanguage('en');
  });

  it('reports the current locale when one is supported', () => {
    const { result } = renderHook(() => useLocale());
    expect(result.current.locale).toBe('en');
    expect(result.current.locales).toContain('mt');
  });

  it('setLocale persists to localStorage and flips i18next', async () => {
    const { result } = renderHook(() => useLocale());

    await act(async () => {
      result.current.setLocale('mt');
    });

    expect(window.localStorage.getItem(LOCALE_STORAGE_KEY)).toBe('mt');
    expect(i18n.language).toBe('mt');
  });

  it('keeps <html lang> in sync on language change', async () => {
    const { result } = renderHook(() => useLocale());

    await act(async () => {
      result.current.setLocale('mt');
    });

    expect(document.documentElement.lang).toBe('mt');

    await act(async () => {
      result.current.setLocale('en');
    });

    expect(document.documentElement.lang).toBe('en');
  });

  it('setLocale is a no-op when the requested locale is already active', async () => {
    const { result } = renderHook(() => useLocale());
    window.localStorage.removeItem(LOCALE_STORAGE_KEY);

    await act(async () => {
      result.current.setLocale('en');
    });

    // Nothing was written — the early-return path in setLocale held.
    expect(window.localStorage.getItem(LOCALE_STORAGE_KEY)).toBeNull();
  });
});
