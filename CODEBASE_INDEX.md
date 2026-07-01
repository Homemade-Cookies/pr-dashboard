# Codebase Index — pr-dashboard (Aspire Team App)

Master navigation for AI agents and new contributors. See also root [`README.md`](README.md)
(authoritative business logic) and [`AGENTS.md`](AGENTS.md) / [`CLAUDE.md`](CLAUDE.md)
(agent instructions).

## What This Is

A team dashboard for the Aspire team that prioritizes GitHub PR review work: focus queue,
unresolved-feedback tracking, merge-blocking policy gate, CI-failing exclusion, and
installable-PWA Web Push notifications. Solution file: `pr-timeline-app.slnx` (3 .NET
projects) + an embedded `frontend/` React app.

## Top-Level Layout

```text
pr-dashboard/
├── pr-timeline-app.slnx              # .NET solution: AppHost, Server, Tests
├── pr-timeline-app.AppHost/          # Aspire orchestration (local + Azure deploy)
├── pr-timeline-app.Server/           # ASP.NET Core minimal API + static file host
├── pr-timeline-app.Tests/            # xUnit.v3 + Aspire.Hosting.Testing
├── frontend/                         # React 19 + TypeScript + Vite PWA
├── .agents/skills/                   # pre-existing, non-orche agent tooling (untouched)
├── .github/workflows/                # ci.yml, deploy.yml (untouched)
├── docs/                             # structured documentation (orche-managed)
├── indexes/                          # targeted navigation indexes (this tree)
├── templates/                        # scaffolding templates matching this repo's patterns
├── project-context.yaml              # orche project context
├── AGENTS.md / CLAUDE.md             # agent instructions (root)
└── README.md                         # authoritative human-facing docs (untouched)
```

## Entry Points

| Concern | File |
|---------|------|
| Backend composition root | `pr-timeline-app.Server/Program.cs` |
| Aspire distributed application definition | `pr-timeline-app.AppHost/AppHost.cs` |
| Frontend app shell | `frontend/src/App.tsx` |
| Frontend entry point | `frontend/src/main.tsx` |
| Service worker (PWA/Web Push) | `frontend/src/sw.ts` |

## Backend Route Groups (`pr-timeline-app.Server/`)

| Route group | File | Service/logic |
|-------------|------|----------------|
| `/api/github/pulls`, `/pulls/graphql`, `/pulls/stream` | `GitHubPullRequestRoutes.cs` | `GitHubPullRequestService.cs`, `GitHubClient.cs` |
| `/api/github/auth-status`, `/login`, `/logout` | `GitHubAuthRoutes.cs` | `GitHubAuthService.cs`, `GitHubOAuthConfiguration.cs`, `GitHubTokenProvider.cs` |
| `/api/notifications/*` | `NotificationRoutes.cs` | `NotificationDetectorService.cs`, `WebPushSender.cs`, `BlobNotificationStore.cs` |
| `/api/app-info` | inline in `Program.cs` | — |
| Global error handling | `GitHubExceptionHandlingExtensions.cs` | Problem Details (RFC 7807) |

Supporting: `GitHubModels.cs`, `GitHubCachePolicy.cs`, `GitHubCacheScopeResolver.cs`,
`GitHubCacheWarmupOptions.cs`, `GitHubPublicCacheWarmupService.cs`,
`GitHubPublicCacheIdentity.cs`, `GitHubResponseCache.cs`, `GitHubReviewPolicyOptions.cs`,
`GitHubHttpRedirects.cs`, `GitHubServiceCollectionExtensions.cs`,
`GitHubPullRequestGraphQlState.cs`, `ReadyToMergeDetection.cs`, `ReviewRequestDetection.cs`,
`NotificationModels.cs`, `NotificationPayloads.cs`, `NotificationUserResolver.cs`,
`NotificationTestRateLimiter.cs`, `NotificationServiceCollectionExtensions.cs`,
`INotificationStore.cs`, `PushSubscriptionEndpointValidator.cs`, `WebPushOptions.cs`,
`Extensions.cs`.

## Frontend Structure (`frontend/src/`)

| Area | Path | Notes |
|------|------|-------|
| Dashboard views + focus logic | `components/dashboard/` | `focusQueue.ts` (business rules), `AttentionBoard.tsx`, `DashboardView.tsx`, `DashboardFilters.tsx`, `FocusExclusionDialog.tsx`, `IssuesOverview.tsx`, `QueueOverview.tsx`, `ShipWeekSection.tsx`, `TileDrilldown.tsx` |
| PR detail views | `components/detail/` | `DetailView.tsx`, `ActivityPanel.tsx`, `ChecksPanel.tsx`, `DeveloperPanel.tsx`, `MilestonePanel.tsx`, `RawActivityTimeline.tsx`, `TriagePanel.tsx` |
| Shared components | `components/` (flat) | `AuthCard.tsx`, `GitHubAvatar.tsx`, `HelpTooltip.tsx`, `NotificationSettings.tsx`, `PullRequestList*.tsx`, `SignalPills.tsx`, `MobileNav.tsx`, loading placeholders |
| Data shaping / logic | `utils/` | `models.ts`, `signals.ts`, `notifications.ts`, `format.ts`, `http.ts`, `loadLifecycle.ts`, `routing.ts`, `useMediaQuery.ts` |
| Types & constants | `types.ts`, `constants.ts` | Shared TS types; default watched repos, team member lists |

## Tests

| Project | Location | Framework |
|---------|----------|-----------|
| Backend | `pr-timeline-app.Tests/*.cs` | xUnit.v3, `Aspire.Hosting.Testing`, coverlet |
| Frontend | co-located `*.test.ts`/`*.test.tsx` under `frontend/src/` | vitest 4.x + jsdom |

## Further Navigation

- [`indexes/TECH_INDEX.md`](indexes/TECH_INDEX.md) — lookup by technology/package
- [`indexes/ARCHITECTURE_INDEX.md`](indexes/ARCHITECTURE_INDEX.md) — lookup by architectural layer
- [`indexes/DOMAIN_INDEX.md`](indexes/DOMAIN_INDEX.md) — lookup by business concept
- [`indexes/QUICK_REF.md`](indexes/QUICK_REF.md) — commands, ports, env vars at a glance
- [`docs/README.codebase-overview.md`](docs/README.codebase-overview.md) — narrative overview
