# AGENTS.md — pr-timeline-app.Server

ASP.NET Core minimal-API backend and, in production, the static file host for the built
React frontend. Target framework `net10.0`. See root `AGENTS.md` for project-wide context.

## Structure

Flat namespace, no layered Accessor/Manager/Engine base classes (confirmed: no such base
classes exist in this project). New capabilities follow the `*Routes.cs` + `*Service.cs`
pair pattern:

- `Program.cs` — composition root: registers services, configures the exception handler,
  authentication, and maps all route groups (`MapGitHubAuthRoutes`,
  `MapGitHubPullRequestRoutes`, `MapNotificationRoutes`) plus `/api/app-info` inline, then
  `UseFileServer()` to serve the built SPA.
- `GitHubPullRequestRoutes.cs` / `GitHubPullRequestService.cs` / `GitHubClient.cs` — PR
  listing (`/api/github/pulls`, `/pulls/graphql`, `/pulls/stream`), backed by
  `GitHubResponseCache.cs` / `GitHubCachePolicy.cs`.
- `GitHubAuthRoutes.cs` / `GitHubAuthService.cs` / `GitHubOAuthConfiguration.cs` /
  `GitHubTokenProvider.cs` — `/api/github/auth-status`, `/login` (OAuth challenge to
  `/signin-github`), `/logout`.
- `NotificationRoutes.cs` / `NotificationServiceCollectionExtensions.cs` /
  `NotificationDetectorService.cs` / `WebPushSender.cs` / `BlobNotificationStore.cs` —
  `/api/notifications/*` (VAPID public key, preferences, subscribe/unsubscribe, test push).
- `GitHubExceptionHandlingExtensions.cs` — the single error-handling entry point, registered
  via `app.UseGitHubApiExceptionHandler()` ahead of `UseAuthentication()`.
- `GitHubReviewPolicyOptions.cs` / `GitHubCacheWarmupOptions.cs` / `WebPushOptions.cs` —
  strongly-typed options bound from `appsettings.json` sections `GitHubReviewPolicy`,
  `GitHubCacheWarmup`, `WebPush`.

## Error Handling (verified against `GitHubExceptionHandlingExtensions.cs`)

All unhandled exceptions are caught by `app.UseExceptionHandler(...)`:

- If the exception is a `GitHubApiException`, the response status code is set to the
  exception's `StatusCode`, and the body is an RFC 7807 Problem Details payload via
  `Results.Problem(title: "GitHub API request failed", detail: <exception.Message>,
  statusCode: <that code>)`.
- Any other exception yields HTTP 500 with `Results.Problem(title: "Unexpected server
  error", detail: "The local backend hit an unexpected error while processing the request.",
  statusCode: 500)`.
- Route-level validation failures (bad `repo`/`state` query params) use
  `Results.ValidationProblem(...)` directly in the route handler — not routed through the
  global exception handler.

Do not introduce a second exception-handling path; extend `GitHubExceptionHandlingExtensions.cs`
or add new typed exceptions that it (or a route-level catch) already understands.

## Do Not

- Don't add MVC controllers — this project is minimal-API only.
- Don't hardcode secrets (`GITHUB_CLIENT_SECRET`, `WebPush:PrivateKey`,
  `GITHUB_PUBLIC_CACHE_TOKEN`). Use `dotnet user-secrets --project pr-timeline-app.Server`
  locally.
- Don't bypass `GitHubReviewPolicyOptions.RequireConversationResolution` — see root
  `AGENTS.md`.
- Don't mix the `github-cache` and `notifications` blob containers.

## Rules to Consult

`backend-engineering/BE-csharp.md`, `backend-engineering/BE-aspnet-rest-api.md`,
`architecture-design/AD-error-handling.md`, `security-privacy/SP-security-best-practices.md`,
`security-privacy/SP-data-privacy.md` (all under
`/Users/ckocheno/neldevsrc/GitHub/nelnet-nbs/orche-infrastructure/rules/`).
