# Codebase Overview — pr-dashboard (Aspire Team App)

## What It Is

A team dashboard for the Aspire team, prioritizing GitHub pull request review work: a focus
queue, unresolved-feedback tracking, a merge-blocking policy gate, CI-failing exclusion, and
Web Push notifications delivered through an installable PWA. See the root
[`README.md`](../README.md) for full behavioral detail.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core minimal APIs, .NET 10 (`net10.0`, preview SDK quality) |
| Orchestration | .NET Aspire (AppHost) |
| Frontend | React 19.2, TypeScript 5.9, Vite 8 |
| PWA / Push | vite-plugin-pwa, Lib.Net.Http.WebPush (VAPID) |
| Storage | Azure Blob Storage (Azurite emulator locally) |
| Auth | GitHub OAuth (AspNet.Security.OAuth.GitHub) |
| Testing | xUnit.v3 + Aspire.Hosting.Testing (backend), vitest 4.x + jsdom (frontend) |
| Deploy target | Azure Container Apps |

## Directory Layout

```text
pr-dashboard/
├── pr-timeline-app.slnx                  # solution: AppHost, Server, Tests
├── pr-timeline-app.AppHost/
│   ├── AppHost.cs                        # distributed application definition
│   └── BlobCommandExtensions.cs          # clear-cache dashboard command
├── pr-timeline-app.Server/
│   ├── Program.cs                        # composition root
│   ├── GitHubPullRequestRoutes.cs / GitHubPullRequestService.cs / GitHubClient.cs
│   ├── GitHubAuthRoutes.cs / GitHubAuthService.cs
│   ├── NotificationRoutes.cs / NotificationDetectorService.cs / WebPushSender.cs
│   ├── GitHubExceptionHandlingExtensions.cs  # global error handling
│   └── appsettings.json                  # WebPush, GitHubCacheWarmup, GitHubReviewPolicy config
├── pr-timeline-app.Tests/                # xUnit.v3 tests
├── frontend/
│   └── src/
│       ├── components/dashboard/         # focus queue UI + focusQueue.ts business logic
│       ├── components/detail/            # PR detail panels
│       ├── components/                   # shared/leaf components
│       └── utils/                        # models.ts, signals.ts, notifications.ts, format.ts
├── .agents/skills/                       # pre-existing, non-orche agent tooling
├── docs/                                 # this documentation tree
└── README.md                             # authoritative human-facing docs
```

## Why It's Structured This Way

- **Flat backend namespace, no layering framework.** `*Routes.cs` + `*Service.cs` pairs keep
  each feature's HTTP surface next to its logic without an Accessor/Manager/Engine
  hierarchy — appropriate for a single-team internal tool of this size.
- **Two separate Blob containers** (`github-cache`, `notifications`) so cache eviction can
  never delete a user's push subscription.
- **Single-replica production constraint** when Web Push is enabled, because the
  notification detector does read-modify-write of per-user dedupe state and currently has no
  leader election.
- **Server hosts the built frontend** (`UseFileServer()`) in production, but the Vite dev
  server is used directly during `aspire start` for fast frontend iteration.

For file-by-file navigation, see the root [`CODEBASE_INDEX.md`](../CODEBASE_INDEX.md).
