# Feature Inventory — pr-dashboard

See [`../../indexes/DOMAIN_INDEX.md`](../../indexes/DOMAIN_INDEX.md) for a file-by-file
index of each feature below. Full behavioral rationale is in the root
[`README.md`](../../README.md).

| Feature | Summary | Key files |
|---|---|---|
| PR dashboard / timeline | Lists and details open/closed PRs across watched repos, with GraphQL and streaming variants | `GitHubPullRequestRoutes.cs`, `GitHubPullRequestService.cs`, `GitHubClient.cs` |
| Focus queue ("Needs attention") | Prioritized queue of PRs the team should review now, with explicit exclusion reasons surfaced to the user | `frontend/src/components/dashboard/focusQueue.ts`, `FocusExclusionDialog.tsx` |
| Unresolved feedback tracking | Flags PRs with open review threads (human or Copilot bot, treated identically); fetched only for reviewed/waiting-on-Copilot PRs to bound GraphQL calls | `GitHubClient.cs`, "Unresolved feedback" bucket in `focusQueue.ts` |
| Merge-blocking policy gate | Per-repo opt-in (`GitHubReviewPolicy:RequireConversationResolution`) for whether unresolved threads block "Ready to merge" | `GitHubReviewPolicyOptions.cs`, `ReadyToMergeDetection.cs` |
| CI-failing exclusion | Excludes PRs with failing HEAD checks from the focus queue (pending is fine); surfaced separately in a "CI failing" bucket | `isChecksFailing` in `frontend/src/utils/models.ts` |
| Ready-to-merge detection | Approved + CI passing + no conflicts + no blocking label + not an aging approval | `ReadyToMergeDetection.cs`, tested in `ReadyToMergeDetectionTests.cs` |
| Ship-week / milestone tracking | Groups PRs/issues by milestone and release branch for release planning | `ship-week` route in `GitHubPullRequestRoutes.cs`, `ShipWeekSection.tsx` |
| Public cache warmup | Serves logged-out users from a shared cache for an allowlisted repo set, without spending user/anonymous quota | `GitHubCacheWarmupOptions.cs`, `GitHubPublicCacheWarmupService.cs` |
| GitHub OAuth login | Scopeless OAuth login for public-repo API reads; dev fallback to local tokens | `GitHubAuthRoutes.cs`, `GitHubOAuthConfiguration.cs` |
| Installable PWA | Installable via browser prompt (desktop/Android) or Add to Home Screen (iOS/Safari, required for push) | `frontend/vite.config.ts` (vite-plugin-pwa), `frontend/src/sw.ts` |
| Web Push notifications | `review_requested` and `ready_to_merge` triggers, VAPID-signed, opt-in, single-replica production constraint | `NotificationRoutes.cs`, `NotificationDetectorService.cs`, `WebPushSender.cs`, `frontend/src/utils/notifications.ts` |
| Developer/triage panels | Per-PR detail views: activity timeline, CI checks, triage state, developer stats, milestone info | `frontend/src/components/detail/*.tsx` |
| App info / build metadata | Exposes the deployed commit SHA for diagnostics | `/api/app-info` in `Program.cs` |

## Cross-Cutting Concerns

- **Error handling**: unified Problem Details (RFC 7807) responses via
  `GitHubExceptionHandlingExtensions.cs` — see
  [`../api/README.api.md`](../api/README.api.md).
- **Caching**: `GitHubCachePolicy.cs` / `GitHubResponseCache.cs` govern PR/issue data
  freshness; see [`../data/README.data.md`](../data/README.data.md).
- **Observability**: OpenTelemetry instrumentation for ASP.NET Core, HTTP, and .NET runtime
  metrics (`OpenTelemetry.*` packages in `pr-timeline-app.Server.csproj`).
