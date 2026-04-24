# 0003 — ASP.NET Core Identity + custom JWT issuance (instead of `MapIdentityApi`)

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

.NET 8 ships `MapIdentityApi<TUser>` which attaches `/register`, `/login`, `/refresh`, `/confirmEmail`, and `/resetPassword` endpoints to the pipeline. It is the fastest way to bolt auth onto a Minimal-API project.

For this project, however:

- Every other endpoint runs through the CQRS pipeline (command → validator → handler → `ErrorOr<T>`).
- The built-in endpoints issue **opaque bearer tokens** by default, not RFC-7519 JWTs, and do not expose the token claims we want (`sub`, `email`, `jti`).
- Refresh-token storage is controlled by Identity internals; we want a dedicated `RefreshTokens` table so we can hash, rotate, and revoke explicitly.

## Decision

Keep ASP.NET Core Identity for its password hashing, lockout, and EF store — but **do not** use `MapIdentityApi`. Instead, hand-roll four commands (`RegisterUser`, `LoginUser`, `RefreshToken`, `RevokeToken`) that wrap `UserManager<ApplicationUser>` via an `IUserService` application-layer seam and issue our own HS256 JWTs through `IJwtTokenService`.

## Consequences

- **Positive:** auth endpoints behave identically to every other endpoint (CQRS, FluentValidation, ProblemDetails); tokens are real JWTs with controlled claims; refresh tokens are explicit aggregates with rotation and revocation semantics.
- **Negative:** ~200 more lines of code than `MapIdentityApi`; password reset / email confirmation are not wired (intentional — out of scope for a trial).
- **Mitigation:** password-reset hooks are trivial follow-ups since Identity's token providers are already configured; the README flags them as deferred work.
