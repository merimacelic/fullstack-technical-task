# 0001 — Clean Architecture with CQRS

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

The trial brief asks for a maintainable, well-structured backend; the job description specifically values **N-tier architecture**, **OO + service-oriented development**, and names DDD as a "great to have." Several architecture styles fit a CRUD API:

- Transaction-script in a single project — fastest to write, hardest to evolve.
- Layered (controllers → services → repositories) — familiar but often drifts into anaemic domain models.
- Clean Architecture with CQRS — clearer dependency rules, explicit intent per request, testable handlers.

## Decision

Split the solution into four projects with strict dependency flow **Domain → Application → Infrastructure → Api**. The Domain has zero external references. CQRS is implemented through the `Mediator` library (see ADR 0002); every request is a `Command` or `Query` with a dedicated handler.

## Consequences

- **Positive:** each handler is a single unit of work with a single public API; validators, logging, and other cross-cutting concerns plug in as pipeline behaviours; architecture tests (NetArchTest) enforce the layering at build time.
- **Negative:** more files per feature than a basic MVC project; new contributors need to learn the commands/queries layout.
- **Mitigation:** an ADR set (this folder) plus the README diagram gives new contributors a 10-minute orientation.
