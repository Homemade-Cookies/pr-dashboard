# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

**pr-dashboard** (product name: **Aspire Team App**) is a team dashboard for the Aspire team
that prioritizes GitHub pull request review work: a focus queue of PRs needing attention,
unresolved-feedback tracking, a merge-blocking policy gate, CI-failing exclusion, and
installable-PWA Web Push notifications.

> **Stack override**: this repo is **not** JS/Node-primary. It is a **.NET 10 / ASP.NET Core
> backend + .NET Aspire orchestration**, with a **React 19 / TypeScript 5.9 / Vite 8**
> frontend embedded under `frontend/`. Any global default assuming "JavaScript/Node.js
> primary" does not apply here — treat C# (`pr-timeline-app.Server`, `pr-timeline-app.AppHost`,
> `pr-timeline-app.Tests`) as the primary backend language and TypeScript/React as the
> primary frontend stack.

---

## Execution Rules — Read First

- **Always execute — never defer.** Do the work directly; don't suggest commands for the
  user to run. Don't claim something "is already configured" without verifying end-to-end.
- **Don't regenerate `README.md`.** It is hand-maintained and authoritative for setup,
  GitHub auth, unresolved-feedback logic, notifications/PWA/VAPID, and deploy steps. Update
  `docs/` for structured/expanded content instead; if `docs/` and `README.md` ever disagree,
  `README.md` wins.
- **Respect the `GitHubReviewPolicy:RequireConversationResolution` gate.** Only repos listed
  there (case-insensitive `owner/repo`, configured in `appsettings.json` /
  `GitHubReviewPolicyOptions.cs`) have unresolved review threads block an approved PR from
  "Ready to merge" (`ReadyToMergeDetection.cs`). Elsewhere it's informational only.
- **Respect the single-replica constraint.** `AppHost.cs` pins the Server to
  `MinReplicas = MaxReplicas = 1` in Azure Container Apps whenever Web Push is enabled,
  because `NotificationDetectorService` does read-modify-write of per-user dedupe state in
  blob storage and assumes it is the only writer. Don't relax this without adding
  single-leader election first.
- **Never hardcode secrets**: `GITHUB_CLIENT_SECRET`, `WebPush:PrivateKey`,
  `GITHUB_PUBLIC_CACHE_TOKEN`. Local dev uses `dotnet user-secrets --project
  pr-timeline-app.Server`; production uses Aspire deploy parameters / environment variables.
- **Symlinks**: a PreToolUse hook auto-resolves symlink targets on Edit/Write. For files that
  may be duplicated across repos, find all copies and confirm the source of truth before
  editing.
- **Tooling order**: LSP (for symbols in `.ts/.tsx/.cs`) → Grep (text) → Glob (filenames) →
  Read (known paths) → Explore subagent (open-ended multi-hop only).
- **Edit protocol**: before modifying a function, find all callers (LSP find-references or
  Grep). Research before you edit.

## Git Workflow

- Run `npm --prefix frontend run lint`, `npm --prefix frontend test`, and
  `dotnet build`/`dotnet test` before committing changes in the corresponding stack.
- Never force-push `main`. Never skip hooks (`--no-verify`).
- Never commit `.env`, `appsettings.*.Local.json`, or any file containing the GitHub OAuth
  client secret, VAPID private key, or `GITHUB_PUBLIC_CACHE_TOKEN`.

## Quick Setup

```bash
aspire start   # local orchestration: Server + Vite frontend + Azurite emulator
# Frontend at http://localhost:5173/
```

## Common Commands

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

# Deploy (Azure Container Apps)
aspire deploy
```

.NET SDK version: **`.NET 10 SDK`** with Aspire CLI from the dev channel (README), pinned in
CI as `dotnet-version: 10.0.x`, `dotnet-quality: preview`. There is no `global.json` in the
repo root — don't add one speculatively; CI and the README are the authoritative version
sources.

## Architecture (summary)

- **`pr-timeline-app.AppHost/`** — .NET Aspire AppHost (`AppHost.cs`,
  `BlobCommandExtensions.cs`). Defines the distributed application: Azurite-backed
  `github-cache` and `notifications` blob containers, the Server project, and (in run mode)
  the Vite frontend as a resource. Handles Azure Container Apps publish-mode wiring
  (OAuth client id/secret, VAPID params, single-replica scale template).
- **`pr-timeline-app.Server/`** — ASP.NET Core minimal-API backend. Flat namespace; **no**
  layered Accessor/Manager/Engine base classes. Composition root is `Program.cs`; routes are
  top-level `*Routes.cs` extension methods (`GitHubPullRequestRoutes.cs`,
  `GitHubAuthRoutes.cs`, `NotificationRoutes.cs`); business logic in `*Service.cs`
  (`GitHubPullRequestService.cs`, `GitHubAuthService.cs`); GitHub HTTP/GraphQL access in
  `GitHubClient.cs`. Global error handling in `GitHubExceptionHandlingExtensions.cs` (see
  Error Handling below). Also hosts the built frontend in production via `UseFileServer()`.
- **`frontend/`** — React 19.2 + TypeScript 5.9 + Vite 8 PWA (`vite-plugin-pwa`, service
  worker at `src/sw.ts`). `src/components/dashboard/` (focus queue, attention board,
  `focusQueue.ts` business logic), `src/components/detail/` (PR detail panels), `src/utils/`
  (`models.ts`, `signals.ts`, `notifications.ts`, `format.ts`).
- **`pr-timeline-app.Tests/`** — xUnit.v3 + `Aspire.Hosting.Testing` smoke/integration tests,
  flat `<Subject>Tests.cs` naming.

Directory-scoped detail: [`AGENTS.md`](AGENTS.md),
[`pr-timeline-app.Server/AGENTS.md`](pr-timeline-app.Server/AGENTS.md),
[`frontend/AGENTS.md`](frontend/AGENTS.md),
[`pr-timeline-app.Tests/AGENTS.md`](pr-timeline-app.Tests/AGENTS.md),
[`docs/AGENTS.md`](docs/AGENTS.md).

## Error Handling

`GitHubExceptionHandlingExtensions.cs` is the single global error-handling entry point
(`app.UseGitHubApiExceptionHandler()`, registered before `UseAuthentication()` in
`Program.cs`). All unhandled exceptions produce an RFC 7807 Problem Details response via
`Results.Problem(...)`:

- `GitHubApiException` → status code from the exception, title `"GitHub API request
  failed"`, detail = exception message.
- Anything else → HTTP 500, title `"Unexpected server error"`, a fixed generic detail
  string (does not leak the underlying exception message).

Route-level input validation (bad `repo`/`state` query params) returns
`Results.ValidationProblem(...)` directly from the route handler, not through this handler.

## Domain Concepts

- **Focus queue ("Needs attention")** — PRs the team should review now. Excludes: unresolved
  feedback, merge conflicts, CI-failing, draft, stalled, and several specialized/community
  lanes. See `frontend/src/components/dashboard/focusQueue.ts`.
- **Unresolved feedback** — PRs with open review threads (human or GitHub Copilot review
  bot, treated identically) are pulled into a dedicated bucket and out of the focus queue.
  Copilot's reviews are filtered out of human review-state calculation but its unresolved
  threads still count.
- **Merge-blocking policy gate** — `GitHubReviewPolicy:RequireConversationResolution`
  (`owner/repo` allowlist, case-insensitive) opts specific repos into unresolved threads
  blocking "Ready to merge" for an approved PR, mirroring GitHub's "require conversation
  resolution" branch protection (not directly readable via this app's token scope).
- **CI-failing exclusion** — PRs with failing checks on HEAD are excluded from "Needs
  attention" (pending checks are fine) but remain visible in a standalone "CI failing"
  bucket.
- **Ready-to-merge detection** (`ReadyToMergeDetection.cs`) — approved, CI not failing, no
  conflicts, no blocking label (`no-merge`/`needs-author-action`), not an aging approval.
- **Web Push notifications** — `review_requested` and `ready_to_merge` triggers, VAPID-signed,
  opt-in (no-op if keys absent), single-replica production constraint (see above).

Full business-logic explanation: root [`README.md`](README.md).

## Tech Stack

.NET 10 (`net10.0`, preview SDK quality) · ASP.NET Core minimal APIs · AspNet.Security.OAuth.GitHub
· Lib.Net.Http.WebPush · Aspire.Azure.Storage.Blobs · OpenTelemetry (ASP.NET Core/HTTP/Runtime)
· xUnit.v3 + Aspire.Hosting.Testing · React 19.2 · TypeScript 5.9 · Vite 8 · vite-plugin-pwa
1.3 · vitest 4.1 · ESLint 9 (flat config) + typescript-eslint 8 · .NET Aspire (AppHost,
distributed application model) · Azurite (local) / Azure Blob Storage (prod) · Azure
Container Apps (deploy target).

## Pre-Existing, Non-Orche Tooling

`.agents/skills/` (`aspire`, `aspire-deployment`, `aspireify`, `dotnet-inspect`,
`playwright-cli`) and `.mcp.json` (`aspire agent mcp`) are pre-existing agent tooling from a
different convention, not managed by orche. Do not move, edit, or delete anything under
`.agents/skills/`.

## Documentation

- [`README.md`](README.md) — authoritative setup, auth, business logic, notifications,
  deploy (do not regenerate).
- [`AGENTS.md`](AGENTS.md) — orche-native cross-cutting instructions and rule pointers.
- [`CODEBASE_INDEX.md`](CODEBASE_INDEX.md) — master navigation index.
- `indexes/` — `TECH_INDEX.md`, `ARCHITECTURE_INDEX.md`, `DOMAIN_INDEX.md`, `QUICK_REF.md`.
- `docs/` — `api/`, `architecture/`, `data/`, `deployment/`, `features/`, `security/`,
  `README.codebase-overview.md`, `README.docs.md`.

## Rules Reference

Applicable rules from
`/Users/ckocheno/neldevsrc/GitHub/nelnet-nbs/orche-infrastructure/rules/`:
`backend-engineering/BE-csharp.md`, `backend-engineering/BE-aspnet-rest-api.md`,
`frontend-engineering/FE-react.md`, `frontend-engineering/FE-pwa.md`,
`devops-infrastructure/DI-docker.md`, `security-privacy/SP-security-best-practices.md`,
`security-privacy/SP-data-privacy.md`, `testing-quality/TQ-csharp-xunit.md`,
`testing-quality/TQ-integration-testing.md`, `testing-quality/TQ-mocking-patterns.md`,
`architecture-design/AD-naming-conventions.md`, `architecture-design/AD-error-handling.md`,
`architecture-design/AD-code-complexity.md`, `documentation-content/DC-markdown.md`,
`documentation-content/DC-mermaid-style-guide.md`.
