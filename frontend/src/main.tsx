import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import { AppProviders } from '@/app/providers';
import { AppRouter } from '@/app/router';
// i18next bootstrap — import for side-effect so `useTranslation()` has its
// resource bundles ready on first render. Must come before the app mounts.
import '@/i18n';
import '@/index.css';

const container = document.getElementById('root');
if (!container) {
  // Not user-visible — this fires before i18n is even loaded, and the HTML
  // root element is a pre-flight invariant, not a runtime state.
  throw new Error('Root container not found — did the HTML template change?');
}

createRoot(container).render(
  <StrictMode>
    <AppProviders>
      <AppRouter />
    </AppProviders>
  </StrictMode>,
);
