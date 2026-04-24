import '@testing-library/jest-dom/vitest';
import { afterAll, afterEach, beforeAll, vi } from 'vitest';
import { cleanup } from '@testing-library/react';
import { server } from './mocks/server';

const noop = (): void => undefined;

// Polyfills jsdom is missing — Radix + Sonner use these at mount time.
globalThis.ResizeObserver ||= class {
  observe = noop;
  unobserve = noop;
  disconnect = noop;
};

if (!('matchMedia' in window)) {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: noop,
      removeEventListener: noop,
      addListener: noop,
      removeListener: noop,
      dispatchEvent: () => false,
    }),
  });
}

// Radix Select scrolls focused items into view; jsdom doesn't implement this.
(Element.prototype as unknown as { scrollIntoView?: () => void }).scrollIntoView ??= vi.fn();

// MSW — start/stop the mock server for the whole test run.
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
  server.resetHandlers();
  cleanup();
});
afterAll(() => server.close());
