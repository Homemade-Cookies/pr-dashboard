# Domain Index — pr-dashboard

Navigation by business concept. Full explanations live in the root [`README.md`](../README.md)
— this index maps each concept to the code that implements it.

## Focus Queue ("Needs attention")

The set of PRs the team should review right now.

- Bucket assignment and exclusion rules:
  `frontend/src/components/dashboard/focusQueue.ts`
  (`excludedFocusBucketLabels`, `disqualifyingFocusBucketLabels`,
  `specializedFocusBucketLabels`, `focusBucketRanks`).
- Supporting predicates: `frontend/src/utils/models.ts`
  (`hasMergeConflicts`, `hasNeedsAuthorActionLabel`, `isAgedOutCommunityPullRequest`,
  `isChecksFailing`, `isCommunityPullRequest`, `isPullRequestWithinFocusAgeLimit`).
- UI: `frontend/src/components/dashboard/AttentionBoard.tsx`,
  `frontend/src/components/dashboard/FocusExclusionDialog.tsx`.

## Unresolved Feedback

PRs with open (unresolved) review threads. Fetched only for PRs that have actually been
reviewed (approved/commented) plus waiting PRs the Copilot bot has reviewed. Human and
Copilot threads are treated identically.

- Backend: `pr-timeline-app.Server/GitHubClient.cs` (`GetReviewStatusAsync` and related
  unresolved-thread GraphQL calls), `GitHubPullRequestService.cs`.
- Frontend bucket: "Unresolved feedback" label in `focusQueue.ts`'s exclusion sets.

## Merge-Blocking Policy Gate

Whether unresolved threads pull an *approved* PR out of "Ready to merge" is opt-in per
repository, because GitHub's branch-protection "require conversation resolution" setting
isn't readable with this app's token scope.

- Config: `GitHubReviewPolicy:RequireConversationResolution` in
  `pr-timeline-app.Server/appsettings.json` (currently `["microsoft/aspire"]`).
- Options binding: `pr-timeline-app.Server/GitHubReviewPolicyOptions.cs`.
- Consumed by: `pr-timeline-app.Server/ReadyToMergeDetection.cs`.

## CI-Failing Exclusion

PRs whose HEAD commit has failing checks are excluded from "Needs attention" (pending checks
are fine) but remain visible in a standalone "CI failing" bucket; they reappear in "Needs
attention" once checks go green.

- Predicate: `isChecksFailing` in `frontend/src/utils/models.ts`.
- Bucket label: `"CI failing"` in `focusQueue.ts`.

## Ready to Merge

Approved, CI not failing, no merge conflicts, no `no-merge`/`needs-author-action` label, not
an aging approval.

- Backend detection: `pr-timeline-app.Server/ReadyToMergeDetection.cs`
  (tested by `pr-timeline-app.Tests/ReadyToMergeDetectionTests.cs`).
- Notification trigger: `ready_to_merge` in `NotificationDetectorService.cs` — nags both
  author and each approver once when it becomes ready, and again only after leaving/re-entering
  that state.

## Public Cache Warmup

Logged-out users read PR data only from a shared public cache, for an allowlisted set of
repositories.

- Config: `GitHubCacheWarmup:*` in `appsettings.json` (`Repositories`, `RefreshIntervalMinutes`,
  `Enabled`/`EnabledInDevelopment`).
- Options: `GitHubCacheWarmupOptions.cs`.
- Warmup service: `GitHubPublicCacheWarmupService.cs`.
- Identity used for the warmup token: `GitHubPublicCacheIdentity.cs`
  (`GITHUB_PUBLIC_CACHE_TOKEN` / `GitHubCacheWarmup:PublicCacheToken`).
- Storage: `github-cache` Azure Blob container (Azurite locally, Azure Blob Storage in prod).

## Web Push Notifications (PWA)

Installable PWA delivering Web Push for `review_requested` and `ready_to_merge` triggers,
even when the app is closed.

- Client subscribe/preferences UI: `frontend/src/components/NotificationSettings.tsx`,
  logic in `frontend/src/utils/notifications.ts`.
- Service worker (push receive/display): `frontend/src/sw.ts`.
- Backend routes: `pr-timeline-app.Server/NotificationRoutes.cs`.
- Detection loop: `NotificationDetectorService.cs`
  (tested by `NotificationDetectorServiceTests.cs`).
- Sending: `WebPushSender.cs` (VAPID-signed via `Lib.Net.Http.WebPush`).
- Storage: `BlobNotificationStore.cs` → `notifications` Blob container (kept separate from
  `github-cache` so cache eviction can never delete a subscription).
- Config: `WebPush:*` in `appsettings.json` / user-secrets (`Enabled`, `PublicKey`,
  `PrivateKey`, `Subject`, `KeyId`).
- Constraint: server pinned to exactly one replica in production while notifications are
  enabled (`AppHost.cs`) — the detector is a single writer for per-user dedupe state.

## GitHub OAuth Login

- Routes: `pr-timeline-app.Server/GitHubAuthRoutes.cs` (`/api/github/auth-status`, `/login`,
  `/logout`; callback at `/signin-github`).
- Config: `GitHubOAuthConfiguration.cs` (`GITHUB_CLIENT_ID`/`GITHUB_CLIENT_SECRET`, or dev
  fallback to `GITHUB_TOKEN`/`GH_TOKEN`/`gh auth token`).
- Requests **no** GitHub OAuth scopes (public repo API reads only).
