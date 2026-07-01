# Deployment — pr-dashboard

Deploy target is **Azure Container Apps**, driven by the Aspire CLI. This document expands
on the root [`README.md`](../../README.md)'s Deploy section — that section is authoritative
if this ever drifts.

## Prerequisites

- Azure CLI, logged in (`az login`)
- Aspire CLI (dev channel)
- An Azure subscription, target region, and resource group
- A GitHub OAuth App (client ID/secret)
- A VAPID key pair for Web Push (see
  [`../security/README.security.md`](../security/README.security.md))

## Deploy Command

```bash
az login
export Azure__SubscriptionId=<subscription-id>
export Azure__Location=<azure-region>
export Azure__ResourceGroup=<resource-group>
export Parameters__github_client_id=<oauth-app-client-id>
export Parameters__github_client_secret=<oauth-app-client-secret>
export Parameters__web_push_public_key=<vapid-public-key>
export Parameters__web_push_private_key=<vapid-private-key>
export Parameters__web_push_subject=mailto:you@example.com
export Parameters__web_push_key_id=<vapid-key-id>

aspire deploy
```

After the first deploy, set the GitHub OAuth App's callback URL to
`https://<aca-fqdn>/signin-github`.

## What `aspire deploy` Provisions

Driven by `pr-timeline-app.AppHost/AppHost.cs`:

- An Azure Container App Environment (`AddAzureContainerAppEnvironment("aca")`).
- Azure Storage with two blob containers: `github-cache`, `notifications` (Azurite locally,
  real Azure Blob Storage in Azure).
- The `pr-timeline-app.Server` project as an Azure Container App:
  - External HTTP endpoint, `/health` health check.
  - Environment variables `GITHUB_CLIENT_ID`, `GITHUB_CLIENT_SECRET`, `GIT_COMMIT_SHA`, and
    `WebPush__*` (Enabled, PublicKey, PrivateKey, Subject, KeyId) sourced from the Aspire
    parameters above.
  - Scale template pinned to `MinReplicas = MaxReplicas = 1` — see
    [`../data/README.data.md`](../data/README.data.md) for why (single-writer notification
    detector).
- The built frontend's static output published into the Server's `wwwroot`
  (`PublishWithContainerFiles`), so in production the Server serves the SPA directly — there
  is no separate frontend container app.

## CI (context, not modified by this documentation pass)

`.github/workflows/ci.yml` runs on every PR/push to `main` (excluding markdown-only changes):
installs the Aspire CLI, runs `aspire doctor`, installs and tests the frontend
(`npm --prefix frontend ci` / `test`), then `dotnet restore/build/test` against
`pr-timeline-app.slnx`. `.github/workflows/deploy.yml` handles the actual deployment
workflow — refer to that file directly for its trigger and steps; it was intentionally left
unexamined and unmodified by this documentation pass.

## Local Equivalent

Local development does not require any of the above — `aspire start` runs the Server, the
Vite frontend, and an Azurite emulator with no Azure credentials needed. GitHub OAuth
falls back to `GITHUB_TOKEN`/`GH_TOKEN`/`gh auth token` in development; Web Push is
opt-in and no-ops entirely if VAPID keys aren't configured via `dotnet user-secrets`.
