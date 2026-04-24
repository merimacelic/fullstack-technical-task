# Task Management — Frontend

React 19 + TypeScript + Redux Toolkit + RTK Query single-page app consuming the .NET 8 TaskManagement API. See the root `README.md` for the full-stack picture; this file is the ops cheat sheet for the frontend.

## Stack

| Concern | Choice |
|---|---|
| Build | Vite 7 |
| Language | TypeScript 5, `@tsconfig/strictest` |
| State | Redux Toolkit 2.x + RTK Query |
| Routing | React Router 7 (library mode) |
| Forms | React Hook Form + Zod |
| UI | Tailwind CSS v4 + shadcn/ui (Radix) |
| Drag-and-drop | @dnd-kit |
| Tests | Vitest + RTL + MSW + Playwright |
| Lint / format | ESLint 9 flat + Prettier + jsx-a11y |
| Package manager | pnpm 10 (pinned via `packageManager`) |

Architecture decisions are captured in `docs/adr/0007`–`0010`.

## Running locally

```bash
# from frontend/
pnpm install
pnpm dev
# → http://localhost:5173
```

The dev server reads `.env.development`. The backend must be reachable on `VITE_API_BASE_URL` (default `http://localhost:8080`). Spin it up with `docker compose up -d api sqlserver` (from the repo root) or `dotnet run --project src/TaskManagement.Api`.

## Scripts

| Script | What it does |
|---|---|
| `pnpm dev` | Vite dev server with HMR |
| `pnpm build` | `tsc -b && vite build` → `dist/` |
| `pnpm preview` | Serve the production build (used by Playwright) |
| `pnpm lint` | ESLint 9 flat config, `--max-warnings=0` |
| `pnpm typecheck` | `tsc -b --noEmit` |
| `pnpm test` | Vitest watch mode |
| `pnpm test:run` | Vitest single run |
| `pnpm test:coverage` | Vitest + V8 coverage → `coverage/` |
| `pnpm test:e2e` | Playwright smoke (needs a live backend) |
| `pnpm format` | Prettier write |

## Tests

- **Unit + component** (`tests/unit/`) — Vitest + React Testing Library + MSW. Run with `pnpm test:run`.
- **E2E** (`tests/e2e/`) — Playwright. Needs the API + SPA running. `pnpm test:e2e:install` then `pnpm test:e2e`.
- **Coverage target:** informative only (no hard gate). `pnpm test:coverage` uploads to CI as an artefact.

## Container build

```bash
docker build -t taskmanagement-frontend:dev frontend
docker run --rm -p 5173:80 -e API_BASE_URL=http://host.docker.internal:8080 taskmanagement-frontend:dev
```

Under docker-compose the runtime `env.js` is written by `docker-entrypoint.sh` from `API_BASE_URL`; the SPA bundle reads `window.__RUNTIME_CONFIG__` before any app code runs, so the same image works in every environment.

## Folder layout

```
src/
├── app/           Redux store, router, providers, hooks, top-level App shell
├── features/
│   ├── auth/
│   ├── tasks/
│   └── tags/
├── shared/
│   ├── ui/        shadcn/ui primitives
│   ├── layout/    AppHeader, AppShell, ThemeProvider, ThemeToggle
│   ├── lib/       config, baseQuery, problemDetails, tokenStorage, cn, date
│   └── hooks/
├── pages/         NotFound, ErrorBoundary
├── main.tsx
└── index.css
tests/
├── setup.ts
├── mocks/         MSW handlers (every endpoint)
├── unit/          Vitest test files
└── e2e/           Playwright specs
```

See ADR 0009 for the reasoning behind feature-sliced *lite*.

## Troubleshooting

- **`pnpm: command not found`** — enable Corepack: `corepack enable && corepack prepare pnpm@10 --activate`. The repo pins pnpm 10 via `package.json#packageManager`.
- **API requests return CORS errors** — the .NET API's `Cors:Origins` already includes `http://localhost:5173`. If you changed the port, add it to `CORS_ORIGINS` in root `.env`.
- **401s every few minutes during active use** — access tokens expire after 15 minutes; the base query mutex refreshes silently. Check the devtools Network panel — the request should be retried after `/api/auth/refresh`.
- **Playwright `webServer` times out** — check nothing else is bound to port 5173, or run `pnpm preview` separately and set `E2E_NO_WEBSERVER=1`.
