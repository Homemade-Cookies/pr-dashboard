# Documentation Guide — pr-dashboard

This `docs/` tree supplements the root [`README.md`](../README.md), which remains the
hand-maintained, authoritative source for setup, GitHub auth, unresolved-feedback business
logic, notifications/PWA/VAPID configuration, and deploy steps.

**Where content overlaps, `README.md` wins.** Files here summarize or add structure around
that same information (e.g. per-endpoint request/response shapes, a container-by-container
storage breakdown) — treat anything here as a snapshot that can drift, not a second source
of truth.

## Structure

| Directory / file | Contents |
|---|---|
| [`README.codebase-overview.md`](README.codebase-overview.md) | Tech stack and directory layout at a glance |
| [`api/README.api.md`](api/README.api.md) | `/api/github/*` and `/api/notifications/*` endpoint reference, actual Problem Details error shape |
| [`architecture/README.architecture.md`](architecture/README.architecture.md) | System architecture, layers, data flow |
| [`data/README.data.md`](data/README.data.md) | Azure Blob Storage container layout, cache policy |
| [`deployment/README.deployment.md`](deployment/README.deployment.md) | `aspire deploy` flow, Azure Container Apps parameters |
| [`features/README.features.md`](features/README.features.md) | Feature inventory with code pointers |
| [`security/README.security.md`](security/README.security.md) | OAuth scope, VAPID key handling, secret management |

## Navigation

For AI-agent-oriented navigation (file-by-file indexes), see the root
[`CODEBASE_INDEX.md`](../CODEBASE_INDEX.md) and `indexes/` directory rather than this `docs/`
tree — `docs/` is written for humans, `indexes/` for quick agent lookups.
