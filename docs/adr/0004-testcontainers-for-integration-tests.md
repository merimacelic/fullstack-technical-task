# 0004 — Testcontainers for integration tests

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

Integration tests need a database that behaves like SQL Server. Options considered:

- **EF InMemory provider** — fast, but too forgiving (case-insensitive Like, relaxed FK enforcement, no real query translation for certain expressions). Good enough for handler unit tests, not for end-to-end HTTP tests.
- **LocalDB / Docker-compose managed DB** — real behaviour, but shared-state hazard across tests and hard to run in CI.
- **SQLite in-memory** — requires a second DbContext configuration and diverges from SQL Server on several features (collations, JSON, `Like`).
- **Testcontainers.MsSql** — spins up a throwaway SQL Server 2022 container per test run, applies migrations, tears down at the end. Works on GitHub Actions' `ubuntu-latest` runners out of the box.

## Decision

Use `Testcontainers.MsSql` 4.x as the integration-test database. Reset state between tests with Respawn.

## Consequences

- **Positive:** tests exercise the actual production query translator, index semantics, and constraint behaviour; zero persistent state between runs; identical behaviour locally and in CI.
- **Negative:** requires Docker on the developer machine and ~20s container boot on first run.
- **Mitigation:** the README calls out Docker as a prerequisite; Testcontainers reuses the container across the xUnit collection fixture so subsequent tests reuse it instantly.
