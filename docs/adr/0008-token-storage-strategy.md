# 0008 — Frontend token storage: access in memory, refresh in localStorage

- **Status:** Accepted
- **Date:** 2026-04-24

## Context

The backend (iteration 2) issues an access token (15 min) + rotating refresh token (7 days) as a JSON body on `/api/auth/register`, `/login`, and `/refresh`. The SPA needs to:

1. Use the access token as `Authorization: Bearer …` on every API call.
2. Survive a full browser reload — users shouldn't have to sign in every time they close a tab.
3. Silently refresh the access token when it expires mid-session.

Three credible patterns in 2026:

| Pattern | Access token | Refresh token | Trade-off |
|---|---|---|---|
| **A. In-memory only** | Redux state | Never stored | Best security; user signs in on every reload |
| **B. In-memory + localStorage** | Redux state | localStorage | This project; XSS-exposed but mitigated |
| **C. In-memory + HttpOnly cookie** | Redux state | HttpOnly cookie set by API | Best security + UX; requires API to set/read cookie |

## Decision

**Pattern B** for iteration 3. The backend currently returns the refresh token in a JSON body, so the SPA writes it to `localStorage` under a single key. `tokenStorage.ts` is the only seam that touches storage; the rest of the app never does.

Pattern C is the 2026-ideal for SPAs; it is documented as a future-work item rather than shipped now because it would require:

1. `/api/auth/register`, `/login`, `/refresh` to set `Set-Cookie: refresh_token=…; HttpOnly; Secure; SameSite=Strict; Path=/api/auth/refresh`.
2. `/api/auth/refresh` to read the cookie instead of a body field.
3. CORS `AllowCredentials` + frontend `fetch(…, { credentials: 'include' })`.

None of that is user-visible, but it is a coordinated change across backend + frontend that is out of scope for iteration 3.

## Mitigations

localStorage is XSS-readable. Our defence-in-depth:

- **Strict CSP** set by nginx (`default-src 'self'; script-src 'self'`) — no inline scripts, no third-party CDN scripts. XSS is the *only* realistic way to reach the token, and CSP closes the most common vectors.
- **No user-supplied HTML** is ever rendered. All user content goes through React's text interpolation (auto-escaped).
- **Rotation + family revocation** on the backend means a leaked refresh token is single-use; replaying a revoked token tears down the entire token family, forcing re-authentication across every device.
- **Access tokens live in memory only** — they are the ones attached to every request, and a page reload drops them on the floor.

## Consequences

- **Positive:** simple, works today, survives reload, single backend contract.
- **Negative:** XSS → one-shot token leak → force re-login; still an attack vector we can tighten.
- **Future work:** swap to HttpOnly cookies in a coordinated backend + frontend change. `tokenStorage.ts` exists partly so this swap touches one file on the SPA side.
