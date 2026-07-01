# Architecture Index — pr-dashboard

Navigation by architectural layer. See [`CODEBASE_INDEX.md`](../CODEBASE_INDEX.md) for the
full tree and [`docs/architecture/`](../docs/architecture/) for narrative detail.

## Orchestration Layer

- `pr-timeline-app.AppHost/AppHost.cs` — defines the distributed application: storage
  emulator/blob containers, the Server project reference, the Vite frontend resource, and
  Azure Container Apps publish-mode configuration (OAuth/VAPID parameters, single-replica
  scale template).
- `pr-timeline-app.AppHost/BlobCommandExtensions.cs` — adds the `clear-cache` custom
  dashboard command to the `github-cache` blob container resource.

## API / Backend Layer

- **Composition root**: `pr-timeline-app.Server/Program.cs` — registers services (Problem
  Details, OpenAPI, keyed blob clients, options binding, GitHub API services, notification
  services), builds the pipeline (`UseGitHubApiExceptionHandler`, `UseAuthentication`,
  `MapOpenApi` in dev), maps all route groups, then serves the built frontend via
  `UseFileServer()`.
- **Routes** (`*Routes.cs`): `GitHubPullRequestRoutes.cs`, `GitHubAuthRoutes.cs`,
  `NotificationRoutes.cs` — each a static class with a `Map*Routes` extension method on
  `IEndpointRouteBuilder`, grouped under `/api/github` or `/api/notifications`.
- **Services** (`*Service.cs`): `GitHubPullRequestService.cs`, `GitHubAuthService.cs` —
  business logic invoked by routes via DI.
- **External integration**: `GitHubClient.cs` — GitHub REST/GraphQL HTTP calls.
- **Cross-cutting**: `GitHubExceptionHandlingExtensions.cs` (global error handling, see
  below), `GitHubCachePolicy.cs` / `GitHubResponseCache.cs` (caching), options classes bound
  from `appsettings.json` (`GitHubCacheWarmupOptions.cs`, `GitHubReviewPolicyOptions.cs`,
  `WebPushOptions.cs`).

This is a **flat namespace with no layered Accessor/Manager/Engine base classes** — verified
via grep, zero hits. New endpoints should follow the `*Routes.cs` + `*Service.cs` pair
pattern (see `templates/backend-route-service-pair/`).

## Error Handling Architecture

Single entry point: `GitHubExceptionHandlingExtensions.cs`, registered as
`app.UseGitHubApiExceptionHandler()` before `UseAuthentication()`. Behavior (verified by
reading the file directly):

1. `GitHubApiException` → HTTP status = exception's `StatusCode`; body = RFC 7807 Problem
   Details (`Results.Problem`) with title `"GitHub API request failed"` and the exception
   message as `detail`.
2. Any other exception → HTTP 500; Problem Details with title `"Unexpected server error"`
   and a fixed, non-leaking detail string.
3. Query-parameter validation (e.g. malformed `repo`/`state`) is handled inline in route
   handlers via `Results.ValidationProblem(...)`, bypassing the global handler entirely.

## Data / Storage Layer

- Two Azure Blob containers, kept deliberately separate:
  - `github-cache` — PR/issue cache and last-good snapshots; subject to the `clear-cache`
    command and TTL eviction.
  - `notifications` — push subscriptions, preferences, per-user dedupe state; never touched
    by cache eviction.
- Local: Azurite emulator with an Aspire data volume (survives container recreation).
  Production: real Azure Blob Storage.

## Frontend Layer

- `frontend/src/App.tsx` — app shell; `main.tsx` — entry point; `sw.ts` — service worker
  (PWA + Web Push receive/display logic).
- `components/dashboard/` — dashboard views, with `focusQueue.ts` holding the focus-queue
  bucket/exclusion business rules (imports from `utils/models.ts`).
- `components/detail/` — single-PR detail views.
- `utils/` — pure logic: PR/attention modeling (`models.ts`), signal-pill dedupe
  (`signals.ts`), Web Push client logic (`notifications.ts`), formatting (`format.ts`), HTTP
  helpers (`http.ts`), navigation (`routing.ts`).
- Built by Vite (`tsc -b && vite build`) and served by the ASP.NET Core Server in production
  (`UseFileServer()`); served by the Vite dev server directly under `aspire start`.

## Test Layer

- `pr-timeline-app.Tests/` — xUnit.v3, some tests backed by `Aspire.Hosting.Testing` to spin
  up (or partially spin up) the distributed application for integration/smoke coverage.
- Frontend: vitest, co-located with source (`*.test.ts`/`*.test.tsx`), jsdom environment.

## Data Flow (high level)

```text
Browser (React SPA)
  → ASP.NET Core minimal API (/api/github/*, /api/notifications/*)
    → GitHubClient.cs → GitHub REST/GraphQL API
    → GitHubResponseCache.cs → github-cache Blob container (Azurite/Azure Blob Storage)
    → BlobNotificationStore.cs → notifications Blob container
  ← Web Push (VAPID-signed) → browser push service → service worker (sw.ts)
```
