# AGENTS.md — pr-dashboard

Instructions for AI coding agents working in this repository. See also `docs/AGENTS.md`,
`pr-timeline-app.Server/AGENTS.md`, `frontend/AGENTS.md`, and `pr-timeline-app.Tests/AGENTS.md`
for directory-scoped detail.

## Project Overview

**Aspire Team App** is a team dashboard for the Aspire team (microsoft/aspire and related
repositories) that helps prioritize GitHub pull request review work. It surfaces a focus
queue of PRs needing attention, tracks unresolved review feedback, gates merge-readiness on
repo-specific branch-protection policy, excludes CI-failing PRs from the focus queue, and
delivers Web Push notifications (as an installable PWA) for review requests and ready-to-merge
PRs. Full behavioral detail is in the root `README.md` — read it before changing business
logic in `focusQueue.ts`, `ReadyToMergeDetection.cs`, or the unresolved-feedback path in
`GitHubClient.cs` / `GitHubPullRequestService.cs`.

## Architecture

Not a JS/Node monorepo. This is a multi-project **.NET 10 solution** (`pr-timeline-app.slnx`)
with an embedded React frontend:

- **`pr-timeline-app.AppHost/`** — .NET Aspire AppHost (`AppHost.cs`). Orchestrates local dev
  (Azurite emulator, the Server project, the Vite frontend) and Azure deployment (`aspire
  deploy` → Azure Container Apps). `BlobCommandExtensions.cs` adds the `clear-cache` dashboard
  command.
- **`pr-timeline-app.Server/`** — ASP.NET Core minimal-API backend and, in production, the
  static file host for the built frontend (`UseFileServer()` in `Program.cs`). Flat namespace;
  **no** layered Accessor/Manager/Engine base classes — routes live in top-level `*Routes.cs`
  extension methods (`GitHubPullRequestRoutes.cs`, `GitHubAuthRoutes.cs`,
  `NotificationRoutes.cs`), business logic in `*Service.cs` files
  (`GitHubPullRequestService.cs`, `GitHubAuthService.cs`), and GitHub HTTP/GraphQL access in
  `GitHubClient.cs`.
- **`frontend/`** — React 19 + TypeScript 5.9 + Vite 8 dashboard UI. `src/components/dashboard`
  (focus queue, attention board, filters), `src/components/detail` (PR detail panels),
  `src/utils` (data shaping: `models.ts`, `focusQueue.ts` is under `components/dashboard/`,
  `signals.ts`, `notifications.ts`, `format.ts`).
- **`pr-timeline-app.Tests/`** — xUnit.v3 tests, some backed by `Aspire.Hosting.Testing` for
  integration/smoke coverage against a running distributed application.

Data flows: browser → Server minimal API (`/api/github/*`, `/api/notifications/*`) → GitHub
REST/GraphQL (via `GitHubClient.cs`, cached in the `github-cache` Azure Blob container) and a
separate `notifications` Blob container for push subscriptions/preferences/dedupe state (kept
separate from the cache so `clear-cache` can never delete a user's subscription).

## Consult These Rules

Before making changes, check the applicable rule file in
`/Users/ckocheno/neldevsrc/GitHub/nelnet-nbs/orche-infrastructure/rules/`:

| Area | Rule file |
|------|-----------|
| C# style/patterns | `backend-engineering/BE-csharp.md` |
| ASP.NET Core minimal API design | `backend-engineering/BE-aspnet-rest-api.md` |
| React components/hooks | `frontend-engineering/FE-react.md` |
| PWA/service worker/Web Push | `frontend-engineering/FE-pwa.md` |
| Container/Azurite/ACA patterns | `devops-infrastructure/DI-docker.md` |
| OAuth tokens, VAPID keys, secrets handling | `security-privacy/SP-security-best-practices.md` |
| PII in notification subscriptions | `security-privacy/SP-data-privacy.md` |
| xUnit test patterns | `testing-quality/TQ-csharp-xunit.md` |
| Aspire-backed integration tests | `testing-quality/TQ-integration-testing.md` |
| Mocking GitHub API in tests | `testing-quality/TQ-mocking-patterns.md` |
| Naming conventions | `architecture-design/AD-naming-conventions.md` |
| Error handling / Problem Details shape | `architecture-design/AD-error-handling.md` |
| Cyclomatic complexity | `architecture-design/AD-code-complexity.md` |
| Markdown formatting | `documentation-content/DC-markdown.md` |
| Mermaid diagram dark-theme styling | `documentation-content/DC-mermaid-style-guide.md` |

## Commands Reference

```bash
# Local orchestration (starts Server, Vite frontend, Azurite via Aspire)
aspire start
# Frontend at http://localhost:5173/

# Build and lint
npm --prefix frontend ci
npm --prefix frontend run lint
npm --prefix frontend run build     # tsc -b && vite build

dotnet restore pr-timeline-app.slnx
dotnet build pr-timeline-app.slnx --no-restore

# Test
npm --prefix frontend test          # vitest run
dotnet test pr-timeline-app.slnx --no-build

# Deploy (Azure Container Apps)
aspire deploy
```

CI (`.github/workflows/ci.yml`) runs, in order: Aspire CLI install + `aspire doctor`,
`npm --prefix frontend ci` + `npm --prefix frontend test`, `dotnet restore/build/test` on
`pr-timeline-app.slnx`. .NET SDK version is pinned in CI as `10.0.x` with
`dotnet-quality: preview` — there is no `global.json` in the repo; treat CI and the README's
".NET 10 SDK" note as authoritative for SDK version, don't add a `global.json` speculatively.

## Do Not

- **Never hardcode GitHub OAuth client secrets, `GITHUB_PUBLIC_CACHE_TOKEN`, or WebPush VAPID
  private keys.** Local dev uses `dotnet user-secrets --project pr-timeline-app.Server`;
  production uses Aspire parameters / environment variables (see `AppHost.cs` and
  `README.md`'s Deploy section).
- **Don't bypass the `GitHubReviewPolicy:RequireConversationResolution` gate logic.** Only
  repos listed there (case-insensitive `owner/repo`) should have unresolved review threads
  pull an approved PR out of "Ready to merge" (`ReadyToMergeDetection.cs`,
  `GitHubReviewPolicyOptions.cs`). Elsewhere the unresolved-feedback signal is informational
  only — don't make it merge-blocking globally.
- **Don't relax the single-replica constraint** in `AppHost.cs`
  (`MinReplicas = MaxReplicas = 1` when Web Push is enabled) without adding single-leader
  election first. The `NotificationDetectorService` does read-modify-write of per-user dedupe
  state in blob storage and assumes it is the only writer.
- **Don't add layered Accessor/Manager/Engine base classes.** This codebase deliberately uses
  flat `*Routes.cs` + `*Service.cs` pairs with minimal APIs — match that pattern for new
  endpoints (see `templates/backend-route-service-pair/`).
- **Don't regenerate or overwrite the root `README.md`.** It is hand-maintained and contains
  the authoritative business-logic explanation (unresolved feedback, merge-blocking gate, CI
  exclusion, notifications). Update `docs/` instead, and treat `docs/` content there as a
  *summary* that can drift — README.md is the source of truth if they disagree.
- **Don't treat the `notifications` and `github-cache` Blob containers as interchangeable.**
  They're intentionally separate so the cache-clear command / TTL eviction can never delete a
  user's push subscription.
- **Don't request additional GitHub OAuth scopes** without updating the README — the app
  currently requests none, relying on public repository API reads only.

## Pre-Existing, Non-Orche Tooling

`.agents/skills/` (aspire, aspire-deployment, aspireify, dotnet-inspect, playwright-cli) is
pre-existing agent tooling from a different tool/convention. It is **not** managed by orche.
Do not move, edit, or delete anything under it. `.mcp.json` configures an `aspire` MCP server
(`aspire agent mcp`) used by that tooling — also out of scope for orche changes.

## Navigation

- `CODEBASE_INDEX.md` — master navigation index
- `indexes/TECH_INDEX.md`, `indexes/ARCHITECTURE_INDEX.md`, `indexes/DOMAIN_INDEX.md`,
  `indexes/QUICK_REF.md` — targeted lookups
- `docs/` — structured documentation (api, architecture, data, deployment, features, security)
