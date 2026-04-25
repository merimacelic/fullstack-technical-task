# Task Management

Full-stack task manager built for the ICON Studios senior full-stack trial. .NET 8 API + React 19 SPA, deployed to Azure.

## Live demo

| | |
|---|---|
| **App** | <https://tasksmt-spa.braveflower-449ecfb3.westeurope.azurecontainerapps.io> |
| **Swagger** | <https://tasksmt-api.braveflower-449ecfb3.westeurope.azurecontainerapps.io/swagger> |
| **Login** | `demo@icon.mt` / `Passw0rd!` |

The demo account is pre-seeded with ~60 tasks, 15 tags, and a mix of statuses, priorities, due dates, and tag associations so the UI has something to demonstrate from the first click.

The first request after a long idle period takes ~10–30 seconds — the API container scales to zero and the SQL database auto-pauses to stay inside Azure's free tier. Subsequent requests are fast.

## Stack

**Backend** — .NET 8, Clean Architecture (Domain → Application → Infrastructure → API), CQRS via [Mediator](https://github.com/martinothamar/Mediator), [ErrorOr](https://github.com/amantinband/error-or) result pattern, FluentValidation, EF Core 8 with SQL Server, ASP.NET Core Identity + JWT bearer with rotating refresh tokens, RFC 7807 problem details, English/Maltese localisation through `IStringLocalizer` + .resx.

**Frontend** — React 19, Vite, TypeScript (strictest), Redux Toolkit + RTK Query, React Hook Form + Zod, Tailwind CSS v4 + shadcn/ui, react-i18next, dnd-kit (drag-and-drop with keyboard support).

**Infrastructure** — Multi-stage Docker images (jammy-chiseled API, nginx-alpine SPA), GitHub Actions CI, GHCR for image hosting, Azure Container Apps + Azure SQL Database for production, Bicep for the deployment.

## Run locally

```bash
cp .env.example .env
# edit .env: set JWT_SECRET_KEY to a 32+ character value
docker compose up -d --build
```

- SPA — <http://localhost:5173>
- API — <http://localhost:8080>
- Swagger — <http://localhost:8080/swagger>
- SQL Server — `localhost:1433` (`sa` / value of `SA_PASSWORD`)

The API runs EF migrations on startup. Set `Seeding__DemoData=true` in the compose env to seed the demo dataset locally too.

## Architecture

```
React SPA (Redux Toolkit + RTK Query)
          │
          │  HTTPS + JWT
          ▼
TaskManagement.Api          minimal APIs, auth, rate limiter, CORS, problem details
          │
          ▼
TaskManagement.Application  CQRS handlers, validators, pipeline behaviors
          │
          ▼
TaskManagement.Domain       aggregates, value objects, smart enums, events
          │
          ▼
TaskManagement.Infrastructure  EF Core, Identity, JWT issuer
          │
          ▼
        SQL Server
```

The dependency arrows are enforced at build time by `LayerDependencyTests` (NetArchTest). Domain has zero external references — no EF, no ASP.NET, no Identity.

## Endpoints

All `/api/tasks/*` and `/api/tags/*` routes require `Authorization: Bearer <accessToken>`. Cross-user access returns 404 (not 403) — never leak whether a task exists.

| Method | Route | Description |
|---|---|---|
| POST | `/api/auth/register` | Create account, issue tokens |
| POST | `/api/auth/login` | Exchange credentials for tokens |
| POST | `/api/auth/refresh` | Rotate refresh token |
| POST | `/api/auth/revoke` | Revoke refresh token (logout) |
| GET | `/api/tasks` | List + filter (`status`, `priority`, `search`, `sortBy`, `tagIds`, `page`, `pageSize`) |
| GET | `/api/tasks/{id}` | Fetch one |
| POST | `/api/tasks` | Create |
| PUT | `/api/tasks/{id}` | Update title, description, priority, due date |
| PATCH | `/api/tasks/{id}/complete` | Mark complete |
| PATCH | `/api/tasks/{id}/reopen` | Reopen |
| PATCH | `/api/tasks/{id}/reorder` | Move to position |
| DELETE | `/api/tasks/{id}` | Delete |

Tags get the equivalent CRUD set under `/api/tags/*`. Health probes at `/health` and `/health/ready` are deliberately omitted from OpenAPI.

A REST Client script that exercises every endpoint lives at [`docs/api.http`](docs/api.http).

## Tests

| Suite | Count | Notes |
|---|---|---|
| Domain unit | 31 | xUnit + Shouldly. Aggregate invariants, smart enums, value objects |
| Application unit | 45 | EF InMemory; every CQRS handler + validator. Includes a concurrency probe for the reorder pipeline |
| Architecture | 7 | NetArchTest layer-dependency rules |
| Integration | 34 | Real SQL Server via Testcontainers. Full HTTP pipeline through `WebApplicationFactory` |
| Reqnroll BDD | 2 | Authenticated task lifecycle in Gherkin |
| Frontend unit | 31 | Vitest + React Testing Library + MSW |
| Frontend e2e | 1 | Playwright smoke (login → create task → complete) |

```bash
dotnet test                                    # everything (needs Docker for integration)
dotnet test tests/TaskManagement.Domain.UnitTests       # domain only, no Docker
cd frontend && pnpm test:run                   # frontend unit
cd frontend && pnpm test:e2e                   # Playwright (needs the API running)
```

## Deployment

The live demo is on Azure Container Apps + Azure SQL Database, both on permanent free tiers. The whole stack is one Bicep template plus a wrapper script — see [`deploy/azure/AZURE.md`](deploy/azure/AZURE.md) for the walkthrough.

CI builds and pushes Docker images to GHCR on every push to `main`. The deploy workflow can either auto-fire after CI (set `AZURE_RESOURCE_GROUP` repo variable + Azure secrets), or be triggered manually with `./deploy.sh`.

## Security

| | |
|---|---|
| Authentication | ASP.NET Core Identity for password hashing + lockout, custom HS256 JWT |
| Refresh tokens | Stored hashed, one-time use, rotate on `/refresh`, revocable |
| Authorization | `.RequireAuthorization()` on protected route groups + per-handler ownership checks |
| Password policy | 8+ chars, upper + lower + digit |
| Brute-force | Identity lockout (5 attempts → 5 minute lockout) + per-IP token bucket on `/api/auth/*` |
| Rate limiting | 100 req/min/IP global |
| CORS | Named policy, origins from config |
| HTTPS / HSTS | Outside Development |
| Headers | `NetEscapades.AspNetCore.SecurityHeaders` (frame-deny, no-sniff, referrer-policy, permissions-policy) |
| Secrets | Never in source: `dotnet user-secrets` locally, env vars in containers, GitHub Actions secrets in CI |
| Static analysis | CodeQL (C# + JS/TS) on every push |

## Architecture decisions

Ten ADRs in [`docs/adr/`](docs/adr/) cover the choices that aren't self-evident from the code: persistence + migration strategy, Mediator over MediatR (licensing), ErrorOr over exceptions, JWT bearer with rotating refresh tokens, Docker base image, Tag aggregate boundary, frontend feature-slice layout, frontend auth strategy, drag-and-drop accessibility.

## Trial brief checklist

| Bonus bullet | Where |
|---|---|
| Unit tests | 100+ tests across .NET + frontend |
| Task prioritisation | `TaskPriority` smart enum, UI badge, sort, filter |
| Drag-and-drop | `OrderKey` algorithm + `PATCH /api/tasks/{id}/reorder` + dnd-kit on the SPA |
| Authentication | JWT + rotating refresh tokens (ADR 0008) |
| Containerisation | Multi-stage Dockerfiles, full-stack `docker compose` |
| Responsive UI | Tailwind breakpoints, mobile sheet filter, theme toggle |
| Deployment readiness | Live on Azure (Container Apps + SQL DB Free Offer), Bicep + GitHub Actions |
| Advanced functionality | Tags second aggregate, CQRS, Clean Architecture, Reqnroll BDD, NetArchTest, English + Maltese localisation, RFC 7807 + i18n |
