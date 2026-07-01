# API Reference — pr-dashboard

Endpoint reference for `pr-timeline-app.Server`. This summarizes behavior visible in the
route files as of this writing; it can drift from the code over time — when in doubt, read
the `*Routes.cs` file directly. See [`../../AGENTS.md`](../../AGENTS.md) for how routes are
organized (flat `*Routes.cs` + `*Service.cs` pairs, no MVC controllers).

## Error Shape (verified against `GitHubExceptionHandlingExtensions.cs`)

All unhandled exceptions are converted to an RFC 7807 Problem Details JSON body by the global
exception handler (`app.UseGitHubApiExceptionHandler()`):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "GitHub API request failed",
  "status": 404,
  "detail": "<GitHubApiException.Message>"
}
```

- If the thrown exception is a `GitHubApiException`, the HTTP status code equals the
  exception's `StatusCode`, and `detail` is the exception's message.
- Any other unhandled exception yields HTTP 500 with title `"Unexpected server error"` and a
  fixed, non-leaking detail string — the actual exception message is never included in the
  response.
- Route-level input validation (malformed `repo`, `state`, `milestone`, or push-subscription
  fields) is returned directly from the handler via `Results.ValidationProblem(...)` — a
  standard ASP.NET Core validation-problem body (`errors` dictionary keyed by field name) —
  and does **not** go through the exception handler above.

## `/api/github/*` (`GitHubPullRequestRoutes.cs`, `GitHubAuthRoutes.cs`)

| Method | Path | Query params | Notes |
|---|---|---|---|
| GET | `/api/github/pulls` | `repo`, `state` (`open`\|`closed`\|`all`, default `open`), `label`, `refresh` | Defaults `repo` to `microsoft/aspire` if omitted |
| GET | `/api/github/pulls/graphql` | same as above | GraphQL-backed snapshot response |
| GET | `/api/github/pulls/stream` | same as above | Streams newline-delimited JSON (`application/x-ndjson`-style) PR entries |
| GET | `/api/github/issues/focus` | `repo`, etc. | Focus-queue issue bucket |
| GET | `/api/github/regression-issues` | same handler as `issues/focus` | Alias route |
| GET | `/api/github/ship-week` | `repo`, `milestone` (required), `releaseBranch`, `refresh` | Returns `ValidationProblem` if `milestone` missing or the service reports validation errors |
| POST | `/api/github/pulls/checks` | `repo`, `refresh`; body: list of PR numbers | Validates PR numbers are `> 0` |
| GET | `/api/github/pulls/{number}/timeline` | `repo` | Single-PR timeline |
| GET | `/api/github/auth-status` | — | Current session's authenticated GitHub identity, if any |
| GET | `/api/github/login` | `returnUrl` (must be a local path) | Issues an OAuth challenge to `/signin-github`; 400 Problem Details if OAuth isn't configured |
| POST | `/api/github/logout` | — | Requires a same-origin/loopback JSON request (CSRF guard); signs out the cookie scheme |

`repo` values must parse as `owner/repo` (`RepositoryName.TryParse`); invalid values return a
`ValidationProblem` with an `errors.repo` message, not a 500.

## `/api/notifications/*` (`NotificationRoutes.cs`)

| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/api/notifications/vapid-public-key` | none | Returns `{ publicKey, keyId }` if Web Push is configured, else `404 Not Found` (used by the client to hide the opt-in UI) |
| GET | `/api/notifications/preferences` | session (401 if absent) | `{ reviewRequested, readyToMerge }` |
| PUT | `/api/notifications/preferences` | session + CSRF guard | Body: `{ reviewRequested, readyToMerge }` |
| POST | `/api/notifications/subscribe` | session + CSRF guard | Body: Web Push `PushSubscription` DTO (`endpoint`, `keys.p256dh`, `keys.auth`, optional `expirationTime`); validates HTTPS endpoint, allowlisted push service, and correct base64url key lengths (p256dh = 65 bytes, auth = 16 bytes); also removes the endpoint from any other user's subscriptions on this device |
| POST | `/api/notifications/unsubscribe` | session + CSRF guard | Body: `{ endpoint }` |
| POST | `/api/notifications/test` | session + CSRF guard | Rate-limited (`NotificationTestRateLimiter`); returns `429` with a `Retry-After` header when throttled, `409 Conflict` Problem Details if push isn't configured, otherwise `{ sent, failed, expired }` |

All mutation routes (`PUT`/`POST`) require `IsBrowserMutationRequest`: a JSON content type
plus an `Origin` header that is either absent, loopback, or same-host — a lightweight CSRF
guard mirrored from the GitHub auth routes' `/logout`.

## `/api/app-info` (inline in `Program.cs`)

| Method | Path | Notes |
|---|---|---|
| GET | `/api/app-info` | Returns `{ commitSha, shortCommitSha, commitUrl }`, derived from the `GIT_COMMIT_SHA` configuration value (`"local"` if unset, in which case `commitUrl` is `null`) |

## Not Covered Here

Detailed request/response DTO shapes for the streaming and ship-week endpoints live in
`GitHubModels.cs` and `NotificationModels.cs`/`NotificationPayloads.cs` — read those directly
for field-level detail rather than relying on this summary.
