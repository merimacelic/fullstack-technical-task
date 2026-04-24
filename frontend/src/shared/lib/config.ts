// Runtime config reader. Production containers write /env.js at startup
// (docker-entrypoint.sh), which sets window.__RUNTIME_CONFIG__ before the app
// boots. In dev the bundle reads import.meta.env instead. A single access
// point means the app never cares which environment it's in.

const runtime = typeof window !== 'undefined' ? window.__RUNTIME_CONFIG__ : undefined;

export const config = {
  apiBaseUrl: runtime?.API_BASE_URL?.trim() || import.meta.env.VITE_API_BASE_URL || '/api',
} as const;

export type AppConfig = typeof config;
