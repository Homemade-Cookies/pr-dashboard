# Security — pr-dashboard

See [`../../AGENTS.md`](../../AGENTS.md) and [`../../CLAUDE.md`](../../CLAUDE.md) for the
"Do Not" list. This document expands on the security-relevant configuration surface.

## GitHub OAuth

- Implemented via `AspNet.Security.OAuth.GitHub`; callback path is `/signin-github`.
- **Requests no GitHub OAuth scopes** — the app only needs public repository API reads, so
  it deliberately avoids asking for `repo` or org-level permissions.
- Production configuration: `GITHUB_CLIENT_ID` / `GITHUB_CLIENT_SECRET` (env vars / Aspire
  deploy parameters, never committed).
- Development fallback (in order, per the root README): an existing OAuth session,
  `GITHUB_TOKEN`, `GH_TOKEN`, or `gh auth token` — see `GitHubTokenProvider.cs` and
  `GitHubOAuthConfiguration.cs`.
- Login/logout mutation routes (`/api/github/logout`, and the notification mutation routes)
  are guarded by a lightweight CSRF check (`IsBrowserMutationRequest`): requires a JSON
  content type and an `Origin` header that is absent, loopback, or same-host. This is not a
  full anti-CSRF token scheme — it relies on the same-origin/JSON-content-type combination
  browsers enforce for cross-site requests.

## Public Cache Token

- `GITHUB_PUBLIC_CACHE_TOKEN` / `GitHubCacheWarmup:PublicCacheToken` — a **server-owned**
  fine-grained PAT or GitHub App token, distinct from any per-user token, used only to warm
  the shared public cache for logged-out users on an allowlisted repo set
  (`GitHubCacheWarmup:Repositories`). Never expose this token to the client.

## Web Push / VAPID

- Web Push is **opt-in**: if `WebPush:PublicKey`/`WebPush:PrivateKey` are absent, every push
  code path no-ops, so the app works fully without any push configuration.
- `WebPush:PrivateKey` is a **secret** — never commit it, never log it, never send it to the
  client. Only `WebPush:PublicKey` (and `WebPush:KeyId`) is shipped to the browser via
  `GET /api/notifications/vapid-public-key`.
- Local development: set via `dotnet user-secrets --project pr-timeline-app.Server`, not
  `appsettings.Development.json`.
- Production: set via Aspire deploy parameters (`Parameters__web_push_private_key`, marked
  `secret: true` in `AppHost.cs`), which become Azure Container Apps environment variables /
  secrets — never plain config files.
- Regenerating the VAPID key pair invalidates all existing subscriptions; users must
  re-subscribe.

## Push Subscription Data (PII-adjacent)

- Push subscription endpoints, keys, and user agent strings are stored in the `notifications`
  Blob container (`BlobNotificationStore.cs`), separate from `github-cache`.
- Subscriptions are validated on write (`NotificationRoutes.cs`): HTTPS-only endpoint,
  allowlisted push service host (`PushSubscriptionEndpointValidator.cs`), and correctly
  sized base64url keys (p256dh 65 bytes, auth 16 bytes) — malformed input is rejected before
  it can reach the encryption layer or corrupt a send cycle.
- Subscribing on a device claims that push endpoint exclusively for the current user,
  removing it from any other user's records — prevents a previous account on a shared
  browser from continuing to receive pushes.
- The test-push route (`POST /api/notifications/test`) is rate-limited per user
  (`NotificationTestRateLimiter.cs`) and returns `429` with `Retry-After` when throttled.

## Secrets Handling Summary

| Secret | Local dev | Production |
|---|---|---|
| `GITHUB_CLIENT_SECRET` | `gh auth token` / `GITHUB_TOKEN` fallback (no OAuth app needed) | Aspire deploy parameter (`secret: true`) |
| `GITHUB_PUBLIC_CACHE_TOKEN` | usually unset (feature is prod-only by default) | Environment variable / Aspire parameter |
| `WebPush:PrivateKey` | `dotnet user-secrets` | Aspire deploy parameter (`secret: true`) |

Never commit any of the above to `appsettings.json`, `appsettings.Development.json`, or any
file tracked by git.

## Single-Replica Constraint as a Security/Integrity Control

Pinning the Server to one Azure Container Apps replica while Web Push is enabled
(`AppHost.cs`) isn't purely a scaling limitation — it's also what keeps the notification
detector's dedupe state consistent (a single writer avoids duplicate pushes from racing
replicas). Treat this as a correctness/integrity constraint, not just a cost optimization,
when considering any change to the deploy topology. See
[`../data/README.data.md`](../data/README.data.md).
