import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';

import { LOCALE_STORAGE_KEY, SUPPORTED_LOCALES, type Locale } from './index';

// Thin wrapper over i18next for locale switching. Persists the chosen locale
// to localStorage so the choice survives reloads, and mirrors the active
// language through react-i18next's context so every useTranslation() consumer
// re-renders in sync.
export function useLocale() {
  const { i18n } = useTranslation();
  const active = (SUPPORTED_LOCALES as readonly string[]).includes(i18n.language)
    ? (i18n.language as Locale)
    : 'en';

  const setLocale = useCallback(
    (next: Locale) => {
      if (next === active) return;
      window.localStorage.setItem(LOCALE_STORAGE_KEY, next);
      void i18n.changeLanguage(next);
    },
    [active, i18n],
  );

  return {
    locale: active,
    setLocale,
    locales: SUPPORTED_LOCALES,
  };
}
