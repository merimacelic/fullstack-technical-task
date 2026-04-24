# 0007 — React SPA with Vite + TypeScript + Redux Toolkit / RTK Query

- **Status:** Accepted
- **Date:** 2026-04-24

## Context

Iteration 3 ships the React frontend the brief asks for. The relevant inputs:

- **Full Stack Trial.pdf** — "React functional components and hooks", "axios or fetch", "React Context API or Redux for state management", responsive UI.
- **Fullstack Developer Job Desc.docx** — lists **React & TypeScript** as required and **Redux** as a "great to have."

2026 realities shaping the stack:

- CRA is deprecated — the React docs point to Vite or a framework (Next.js, React Router framework mode). The backend is a separate .NET API and there is no SSR requirement, so a pure SPA (Vite + React Router v7 in *library* mode) is the right signal, not a framework.
- Redux Toolkit 2.x is the modern Redux. Writing classic Redux with hand-rolled actions/reducers in 2026 would be a red flag.
- Server-state caching, invalidation, and refresh-on-focus are a solved problem — either TanStack Query or RTK Query. Duplicating server data into Redux slices is the 2019 pattern.

## Decision

- **Vite 7 + React 19 + TypeScript 5** with `@tsconfig/strictest`. React Compiler enabled via Babel plugin for automatic memoisation.
- **Redux Toolkit 2.x** for client state (auth, UI). **RTK Query** for every server call (tasks, tags, auth endpoints). One `createApi` base; feature modules attach endpoints via `injectEndpoints`.
- **React Router v7** in library mode (`createBrowserRouter` + `RouterProvider`). A top-level `<ProtectedRoute>` gates authenticated routes with a silent refresh on bootstrap.
- **React Hook Form + Zod** for every form; Zod schemas mirror the server-side FluentValidation rules so feedback lands before the round-trip.
- **Tailwind CSS v4 + shadcn/ui** (copy-in, Radix-based primitives) for the UI baseline.
- **@dnd-kit** for drag-and-drop (keyboard + ARIA live-region a11y out of the box).
- **Vitest + React Testing Library + MSW** for unit/component tests; **Playwright** for a 3-flow smoke suite.
- **pnpm 10** (pinned via `package.json#packageManager`) for dependency management.

## Why RTK Query over TanStack Query + Zustand

TanStack Query is the 2026 default when the brief doesn't call out a state container. The trial explicitly lists Redux as a "great to have", so RTK Query is the correct Redux-flavoured answer: same caching / invalidation / refetch-on-focus story, integrated with Redux DevTools, and a single middleware registration.

## Why library mode, not framework mode (React Router v7)

Framework mode ships SSR loaders, Remix-style route modules, and server-side code splitting — all useful when the router owns the server. Here the server is .NET with JWT. Running an extra Node SSR layer for a CRUD SPA would be friction-for-friction's-sake.

## Consequences

- **Positive:** small, opinionated stack that matches both the brief and 2026 norms. TypeScript strictest + ESLint jsx-a11y + React Compiler bakes in quality from day 1. One-command dev (`pnpm dev`), one-command docker (`docker compose up`).
- **Negative:** more moving parts than a single-file prototype. RTK Query has a learning curve for devs only familiar with classic Redux.
- **Mitigation:** feature-sliced folder structure colocates everything per feature; the shared `api.ts` + `baseQuery.ts` are ~40 lines each — the whole pattern is readable in one sitting.
