# AGENTS.md — templates

Scaffolding templates matching this codebase's actual conventions. See
`README.templates.md` for usage and `structure.yml` for the manifest.

## Do Not

- Don't add generic/framework-agnostic templates unrelated to this repo's actual patterns
  (minimal-API route+service pairs, React function components under
  `frontend/src/components/`). Keep this directory small and specific to pr-dashboard.
- Don't introduce layered Accessor/Manager/Engine scaffolding — this codebase's backend
  pattern is flat `*Routes.cs` + `*Service.cs`.

## Rules to Consult

`backend-engineering/BE-aspnet-rest-api.md`, `frontend-engineering/FE-react.md` (under
`/Users/ckocheno/neldevsrc/GitHub/nelnet-nbs/orche-infrastructure/rules/`).
