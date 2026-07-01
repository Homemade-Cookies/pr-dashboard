# AGENTS.md — pr-timeline-app.Tests

xUnit.v3 test project (`xunit.v3` + `xunit.runner.visualstudio` + `coverlet.collector`),
target framework `net10.0`. References both `pr-timeline-app.AppHost` and
`pr-timeline-app.Server` project. Uses `Aspire.Hosting.Testing` for tests that stand up (or
partially stand up) the distributed application.

## Structure

- `GitHubApiSmokeTests.cs` — smoke-level checks against the GitHub API integration surface.
- `GitHubCachePolicyTests.cs`, `GitHubClientTests.cs` — cache policy and HTTP/GraphQL client
  behavior.
- `NotificationDetectorServiceTests.cs`, `NotificationModelTests.cs`,
  `NotificationRouteLoggingTests.cs`, `NotificationStoreTests.cs` — notification detection,
  models, route logging, and blob-backed store behavior.
- `PullRequestSummaryTests.cs`, `ReadyToMergeDetectionTests.cs` — PR summary shaping and the
  `ReadyToMergeDetection.cs` business rule (approved, non-failing CI, no conflicts, no
  blocking label, not an aging approval).

Global usings (from the `.csproj`): `System.Net`, `Aspire.Hosting`,
`Aspire.Hosting.ApplicationModel`, `Aspire.Hosting.Testing`, `Xunit`.

## Commands

```bash
dotnet test pr-timeline-app.slnx --no-build
```

CI runs this with `--logger "console;verbosity=detailed"` after `dotnet build`.

## Conventions

- New test files should follow the existing `<Subject>Tests.cs` naming and live flat in this
  directory — there is no nested `Unit/`/`Integration/` split today.
- Tests that need a real GitHub API surface or blob storage should prefer the existing
  fakes/mocks pattern demonstrated in `GitHubClientTests.cs` / `NotificationStoreTests.cs`
  over adding new external dependencies.

## Rules to Consult

`testing-quality/TQ-csharp-xunit.md`, `testing-quality/TQ-integration-testing.md`,
`testing-quality/TQ-mocking-patterns.md` (under
`/Users/ckocheno/neldevsrc/GitHub/nelnet-nbs/orche-infrastructure/rules/`).
