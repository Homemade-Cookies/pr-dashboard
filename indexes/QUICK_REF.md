# Quick Reference — pr-dashboard

## Run Locally

```bash
aspire start
# Frontend: http://localhost:5173/
```

## Build / Lint / Test

```bash
# Frontend
npm --prefix frontend ci
npm --prefix frontend run lint
npm --prefix frontend run build     # tsc -b && vite build
npm --prefix frontend test          # vitest run

# Backend
dotnet restore pr-timeline-app.slnx
dotnet build pr-timeline-app.slnx --no-restore
dotnet test pr-timeline-app.slnx --no-build
```

## Deploy

```bash
az login
export Azure__SubscriptionId=<subscription-id>
export Azure__Location=<azure-region>
export Azure__ResourceGroup=<resource-group>
export Parameters__github_client_id=<oauth-app-client-id>
export Parameters__github_client_secret=<oauth-app-client-secret>
export Parameters__web_push_public_key=<vapid-public-key>
export Parameters__web_push_private_key=<vapid-private-key>
export Parameters__web_push_subject=mailto:you@example.com
export Parameters__web_push_key_id=<vapid-key-id>

aspire deploy
```

After first deploy: set GitHub OAuth callback to `https://<aca-fqdn>/signin-github`.

## Key Environment Variables / Config Keys

| Variable / Config key | Purpose |
|---|---|
| `GITHUB_CLIENT_ID` / `GITHUB_CLIENT_SECRET` | GitHub OAuth App (production) |
| `GITHUB_TOKEN` / `GH_TOKEN` / `gh auth token` | Dev-only auth fallback |
| `GITHUB_PUBLIC_CACHE_TOKEN` / `GitHubCacheWarmup:PublicCacheToken` | Server-owned token for public cache warmup |
| `GitHubCacheWarmup:Repositories` | Allowlisted repos for logged-out public cache reads |
| `GitHubReviewPolicy:RequireConversationResolution` | `owner/repo` allowlist gating merge-blocking on unresolved threads |
| `WebPush:Enabled` / `PublicKey` / `PrivateKey` / `Subject` / `KeyId` | VAPID Web Push config |
| `GIT_COMMIT_SHA` | Surfaced via `/api/app-info` |

## Default Watched Repositories

`microsoft/aspire`, `microsoft/aspire.dev`, `microsoft/aspire-skills`, `microsoft/dcp`,
`CommunityToolkit/Aspire` (user-overridable to any `owner/repo` list in the dashboard UI).

## Key API Routes

| Route | File |
|---|---|
| `GET /api/github/pulls` | `GitHubPullRequestRoutes.cs` |
| `GET /api/github/pulls/graphql` | `GitHubPullRequestRoutes.cs` |
| `GET /api/github/pulls/stream` | `GitHubPullRequestRoutes.cs` |
| `GET /api/github/auth-status` | `GitHubAuthRoutes.cs` |
| `GET /api/github/login` | `GitHubAuthRoutes.cs` (OAuth challenge) |
| `POST /api/github/logout` | `GitHubAuthRoutes.cs` |
| `GET /api/notifications/vapid-public-key` | `NotificationRoutes.cs` |
| `GET`/`PUT /api/notifications/preferences` | `NotificationRoutes.cs` |
| `GET /api/app-info` | `Program.cs` (inline) |

## Solution / Projects

| Project | Path |
|---|---|
| AppHost | `pr-timeline-app.AppHost/pr-timeline-app.AppHost.csproj` |
| Server | `pr-timeline-app.Server/pr-timeline-app.Server.csproj` |
| Tests | `pr-timeline-app.Tests/pr-timeline-app.Tests.csproj` |

## Prerequisites

.NET 10 SDK · Aspire CLI (dev channel) · Docker (or another Aspire container runtime, for
Azurite) · Node.js `20.19+`/`22.12+` · optional `gh` CLI · optional Azure CLI.
