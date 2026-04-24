# 0009 — Feature-sliced *lite* structure for the React SPA

- **Status:** Accepted
- **Date:** 2026-04-24

## Context

Three common layouts for a React SPA of this size (3–4 features, ~25 components):

1. **By-type** (`components/`, `hooks/`, `pages/`, `store/`) — familiar but fragments a feature across five top-level folders; every change touches many places.
2. **Feature-Sliced Design (FSD, full)** — layers × slices (`app`, `processes`, `pages`, `widgets`, `features`, `entities`, `shared`). Strong guarantees, but overkill for a trial.
3. **Feature-sliced *lite*** — `app/`, `features/{feature}/`, `shared/`, `pages/` (thin). Colocates per-feature code; one shared `ui/` for design-system primitives.

## Decision

Adopt feature-sliced lite:

```
src/
├── app/           Redux store, router, providers, hooks, top-level App.tsx
├── features/      Each feature is a self-contained bundle
│   ├── auth/      api.ts, slice.ts, schemas.ts, components/, pages/, ProtectedRoute.tsx
│   ├── tasks/     api.ts, slice.ts, schemas.ts, components/, pages/, hooks/, types.ts
│   └── tags/      api.ts, schemas.ts, components/, types.ts
├── shared/        Cross-cutting — design system, utilities, hooks
│   ├── ui/        shadcn/ui copy-ins (Radix-based primitives)
│   ├── layout/    AppHeader, AppShell, ThemeProvider, ThemeToggle
│   ├── lib/       config, baseQuery, api, problemDetails, tokenStorage, cn, date
│   └── hooks/     useDebouncedValue, useOnlineStatus
└── pages/         Only non-feature pages (NotFound, ErrorBoundary)
```

Rules:

- **Features may import from `shared/`, never from each other.** If two features need the same thing, move it to `shared/`. The one exception: tasks imports the tags API (TagPicker lives inside TaskForm); this is a deliberate composition, not a dependency leak.
- **`app/` depends on features**, not the other way around. Features don't know the store exists until the store imports their reducer.
- **RTK Query endpoints attach to a single `shared/lib/api.ts` base** via `injectEndpoints`. One middleware registration, one devtools panel.

## Consequences

- **Positive:** any change to a feature is a single-folder change. Dead-code detection on a feature is easy — delete the folder. Good match for future feature flags (wrap an entire feature's `index.ts` export).
- **Negative:** introduces an opinionated line where a new engineer might put things in `shared/` that would be better scoped to a feature. Lint rule or review can catch drift.
- **Deferred:** we don't ship `entities/` or `widgets/` as full FSD prescribes — the trial doesn't have that complexity. If a second consumer of `TaskCard` emerges, it gets promoted to `shared/ui/`.
