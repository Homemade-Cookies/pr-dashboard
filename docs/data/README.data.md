# Data Layer — pr-dashboard

Storage is entirely Azure Blob Storage (Azurite emulator locally). There is no relational
database in this codebase.

## Blob Containers

Defined in `pr-timeline-app.AppHost/AppHost.cs`, both carved from a single Azure Storage
resource (`storage`), which runs as the Azurite emulator locally with a data volume so cache
snapshots survive container recreation:

| Container | Purpose | Written by | Read by |
|---|---|---|---|
| `github-cache` | PR/issue cache and last-good snapshots for the dashboard | `GitHubResponseCache.cs`, `GitHubPublicCacheWarmupService.cs` | `GitHubPullRequestService.cs`, `GitHubClient.cs` |
| `notifications` | Push subscriptions, per-user preferences, per-user dedupe state, user profiles | `BlobNotificationStore.cs` | `NotificationDetectorService.cs`, `NotificationRoutes.cs` |

**These are intentionally separate.** The `clear-cache` custom Aspire dashboard command
(`BlobCommandExtensions.cs`) and any TTL-based eviction target `github-cache` only — the
`notifications` container is never touched by that path, so a cache reset can never delete a
user's push subscription.

## Cache Policy

- `GitHubCachePolicy.cs` / `GitHubCacheScopeResolver.cs` govern how PR/issue data is cached
  and scoped (e.g. per-repository, per-auth-context).
- `GitHubResponseCache.cs` is the read/write surface used by `GitHubPullRequestService.cs`.
- Logged-out users only ever read from the public cache for repositories listed in
  `GitHubCacheWarmup:Repositories` (`appsettings.json`); `GitHubPublicCacheWarmupService.cs`
  refreshes that cache on `GitHubCacheWarmupOptions.RefreshIntervalMinutes`, authenticated
  with `GITHUB_PUBLIC_CACHE_TOKEN` / `GitHubCacheWarmup:PublicCacheToken` — a server-owned
  fine-grained PAT or GitHub App token, never a per-user token.

## Notification Data Model

`INotificationStore.cs` defines the storage contract; `BlobNotificationStore.cs` implements
it against the `notifications` container. Records include (see `NotificationModels.cs` for
exact shapes):

- User profile (`Id`, `Login`, `UpdatedAt`)
- Notification preferences (`ReviewRequested`, `ReadyToMerge` booleans)
- Push subscriptions (`Endpoint`, `P256dh`, `Auth`, optional `ExpirationTime`, `KeyId`,
  `UserAgent`, timestamps) — validated on write by `NotificationRoutes.cs` (HTTPS endpoint,
  allowlisted push service via `PushSubscriptionEndpointValidator.cs`, correct base64url key
  lengths: p256dh 65 bytes, auth 16 bytes)
- Per-user dedupe state consumed by `NotificationDetectorService.cs` to avoid re-notifying
  for a PR that's still in the same state

Subscribing to push on a device removes that endpoint from any other user's subscription
records, so a previous account on a shared browser stops receiving pushes meant for someone
else (see the comment in `NotificationRoutes.cs`'s `subscribe` handler).

## Single-Writer Constraint

`NotificationDetectorService.cs` performs read-modify-write cycles against per-user dedupe
state. In production, `AppHost.cs` pins the Server to exactly one Azure Container Apps
replica (`MinReplicas = MaxReplicas = 1`) whenever Web Push is enabled, so this loop has a
single writer. ETag/`If-Match` writes provide defense-in-depth but are not a substitute for
this constraint — see `docs/security/README.security.md` and the root
[`README.md`](../../README.md)'s Notifications section for the full rationale.
