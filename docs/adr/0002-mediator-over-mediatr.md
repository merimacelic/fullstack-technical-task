# 0002 — `Mediator` (martinothamar) over MediatR

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

Historically MediatR was the default CQRS mediator in .NET. MediatR v13 (2024) moved to a commercial license for larger teams, making it awkward for OSS projects and interview repos.

## Decision

Use [`Mediator`](https://github.com/martinothamar/Mediator) by Martin Othamar. It is source-generated (no runtime reflection for the hot path), MIT-licensed, and API-compatible enough that moving back to MediatR is a search/replace if the licensing picture changes.

## Consequences

- **Positive:** zero runtime allocations for common dispatch paths, free licence, actively maintained.
- **Negative:** pipeline-behaviour delegate signature differs from MediatR (`CancellationToken` comes before `MessageHandlerDelegate`); handlers are not registered as open `IRequestHandler<,>` so a DI validation test must probe `ISender` instead of the individual handler types.
- **Mitigation:** the difference is captured in the pipeline-behaviour file comments and the DI container validation test (`DiContainerValidationTests.cs`) explicitly resolves `ISender`.
