# 0005 — `jammy-chiseled-extra` container base image

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

The API is published as a Linux container. .NET 8 offers several base images:

- `aspnet:8.0` — full Debian base, ~210 MB, shell + package manager, runs as root.
- `aspnet:8.0-jammy` — Ubuntu variant of the above.
- `aspnet:8.0-jammy-chiseled` — Microsoft's "distroless" Ubuntu, ~113 MB, no shell, no package manager, runs as non-root (UID 1654). Invariant globalization mode.
- `aspnet:8.0-jammy-chiseled-extra` — same as chiseled, **plus** ICU and tzdata (~150 MB).
- `aspnet:8.0-alpine` — smaller, musl-based; some libraries (SqlClient especially) have been historically wobbly on musl.

`Microsoft.Data.SqlClient` requires ICU — running it on plain `-chiseled` throws `Globalization Invariant Mode is not supported` on first DB connect.

## Decision

Use `mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled-extra` as the runtime base.

## Consequences

- **Positive:** ~150 MB image (vs 210 MB full), non-root by default, no shell or package manager in the final image ⇒ smaller attack surface, ICU available for SqlClient.
- **Negative:** no shell means classic `HEALTHCHECK ["CMD-SHELL", "wget ..."]` won't work. Health checks must be done via the HTTP `/health` endpoint from the orchestrator (Kubernetes `httpGet`, load-balancer probe).
- **Mitigation:** the `/health` and `/health/ready` endpoints are exposed specifically for this. The `docker-compose.yml` omits container-level HEALTHCHECK and documents the reason inline.
