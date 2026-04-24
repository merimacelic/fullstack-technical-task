// i18next bootstrap. Runs once, before the app mounts (import side-effect in
// src/main.tsx). Language detection order: explicit localStorage choice →
// browser navigator language → fallback 'en'. The detected language is also
// written to document.documentElement.lang so screen readers announce content
// in the correct locale.

import i18n from 'i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import { initReactI18next } from 'react-i18next';

import en from './locales/en.json';
import mt from './locales/mt.json';

export const SUPPORTED_LOCALES = ['en', 'mt'] as const;
export type Locale = (typeof SUPPORTED_LOCALES)[number];

export const LOCALE_STORAGE_KEY = 'task-management.locale';

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      mt: { translation: mt },
    },
    supportedLngs: [...SUPPORTED_LOCALES],
    fallbackLng: 'en',
    // React already escapes rendered content, so i18next's extra pass would
    // double-escape characters in interpolated task titles / tag names.
    interpolation: { escapeValue: false },
    detection: {
      order: ['localStorage', 'navigator'],
      lookupLocalStorage: LOCALE_STORAGE_KEY,
      caches: ['localStorage'],
    },
    returnNull: false,
  });

// Keep <html lang="..."> in sync so assistive tech picks up the right dictionary.
if (typeof document !== 'undefined') {
  document.documentElement.lang = i18n.language;
  i18n.on('languageChanged', (lng) => {
    document.documentElement.lang = lng;
  });
}

export default i18n;
