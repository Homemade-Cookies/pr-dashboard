# Templates — pr-dashboard

Scaffolding templates matching this codebase's actual conventions. See
[`structure.yml`](structure.yml) for the manifest, [`AGENTS.md`](AGENTS.md) for agent
guidance.

## Available Templates

### `backend-route-service-pair/`

A new ASP.NET Core minimal-API route group + service, matching the flat `*Routes.cs` +
`*Service.cs` pattern used throughout `pr-timeline-app.Server` (e.g.
`GitHubPullRequestRoutes.cs`/`GitHubPullRequestService.cs`). This codebase has **no**
layered Accessor/Manager/Engine base classes — don't introduce that pattern.

To use:

1. Copy `__Feature__Routes.cs.template` and `__Feature__Service.cs.template` into
   `pr-timeline-app.Server/`, renaming `__Feature__` to your feature name and dropping the
   `.template` extension.
2. Replace the placeholder route path (`/api/feature`) and request/response records.
3. Register the new route group in `Program.cs` alongside the existing `Map*Routes()` calls.
4. If it calls GitHub's API, throw `GitHubApiException` on failure so
   `GitHubExceptionHandlingExtensions.cs` translates it into a Problem Details response with
   the right status code — don't add a second error-handling path.
5. Add tests in `pr-timeline-app.Tests/` following the `<Subject>Tests.cs` naming.

### `react-component/`

A new React function component matching the style used across `frontend/src/components/`
(typed props object, named function, default export — see `LoadingMetric.tsx`,
`SignalPills.tsx`).

To use:

1. Copy `ComponentName.tsx.template` (and, if the component has logic worth testing,
   `ComponentName.test.tsx.template`) into the appropriate directory:
   - `frontend/src/components/dashboard/` — dashboard-specific
   - `frontend/src/components/detail/` — PR-detail-specific
   - `frontend/src/components/` — shared/leaf components
2. Rename `ComponentName` throughout and drop the `.template` extension.
3. Keep pure logic (data shaping, business rules) in `frontend/src/utils/` or, for
   focus-queue rules specifically, `components/dashboard/focusQueue.ts` — not inline in the
   component body.
4. Run `npm --prefix frontend run lint` and `npm --prefix frontend test` before committing.

## Adding a New Template

Only add a template if it captures a pattern that's already repeated at least twice in this
codebase. Update `structure.yml` with the new entry.
