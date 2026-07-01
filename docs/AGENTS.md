# AGENTS.md — docs

Structured documentation supplementing the root `README.md`. The root `README.md` is the
hand-maintained source of truth for setup, GitHub auth, unresolved-feedback business logic,
notifications/PWA/VAPID configuration, and deploy steps — it is intentionally left untouched
by orche initialization.

## Structure

- `README.codebase-overview.md` — tech stack and directory layout summary.
- `README.docs.md` — how this docs tree is organized.
- `api/` — endpoint documentation for `/api/github/*` and `/api/notifications/*`.
- `architecture/` — system architecture, data flow, project dependencies.
- `data/` — Azure Blob Storage container layout (`github-cache`, `notifications`) and cache
  policy.
- `deployment/` — `aspire deploy` flow, Azure Container Apps parameters/env vars.
- `features/` — feature inventory (focus queue, unresolved feedback, notifications, etc.).
- `security/` — GitHub OAuth scope, VAPID key handling, token/secret management.

## Do Not

- Don't let content here contradict `README.md`. Where a doc here summarizes behavior also
  described in `README.md` (unresolved-feedback rules, notification triggers, deploy env
  vars), treat `README.md` as authoritative if they ever diverge, and update this doc to
  match rather than the reverse.
- Don't duplicate the full text of `README.md` — link to it and add detail/structure it
  doesn't have room for (e.g. endpoint-by-endpoint request/response shapes).

## Rules to Consult

`documentation-content/DC-markdown.md`, `documentation-content/DC-mermaid-style-guide.md`
(under `/Users/ckocheno/neldevsrc/GitHub/nelnet-nbs/orche-infrastructure/rules/`).
