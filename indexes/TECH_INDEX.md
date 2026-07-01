# Tech Index — pr-dashboard

Navigation by technology/package. See [`CODEBASE_INDEX.md`](../CODEBASE_INDEX.md) for the
full tree.

## .NET 10 / C# (backend)

| Package | Where used |
|---------|------------|
| `Microsoft.NET.Sdk.Web` | `pr-timeline-app.Server/pr-timeline-app.Server.csproj` |
| `AspNet.Security.OAuth.GitHub` | `GitHubAuthRoutes.cs`, `GitHubOAuthConfiguration.cs` |
| `Lib.Net.Http.WebPush` | `WebPushSender.cs`, `WebPushOptions.cs` |
| `Aspire.Azure.Storage.Blobs` | `Program.cs` (`AddKeyedAzureBlobContainerClient`), `BlobNotificationStore.cs`, `GitHubResponseCache.cs` |
| `Microsoft.AspNetCore.OpenApi` | `Program.cs` (`AddOpenApi`, `MapOpenApi` in Development) |
| `Microsoft.Extensions.Http.Resilience` | `GitHubServiceCollectionExtensions.cs` (HTTP client resilience) |
| `Microsoft.Extensions.ServiceDiscovery` | Aspire service discovery wiring |
| `OpenTelemetry.*` (AspNetCore, Http, Runtime instrumentation) | service defaults / `Program.cs` |

## .NET Aspire

| Concern | File |
|---------|------|
| Distributed application definition | `pr-timeline-app.AppHost/AppHost.cs` |
| Azurite emulator + blob containers (`github-cache`, `notifications`) | `AppHost.cs` |
| Custom `clear-cache` dashboard command | `pr-timeline-app.AppHost/BlobCommandExtensions.cs` |
| Azure Container Apps publish wiring (params, single-replica scale) | `AppHost.cs` (`PublishAsAzureContainerApp`) |
| Vite frontend as an Aspire resource | `AppHost.cs` (`AddViteApp`) |
| Aspire-backed test host | `pr-timeline-app.Tests/*.cs` (`Aspire.Hosting.Testing`) |

## Testing

| Framework | Project | Notes |
|-----------|---------|-------|
| xUnit.v3 + xunit.runner.visualstudio + coverlet.collector | `pr-timeline-app.Tests` | `net10.0`, references AppHost + Server projects |
| vitest 4.x (jsdom) | `frontend/` | `npm test` → `vitest run`; co-located `*.test.ts(x)` files |

## React / TypeScript / Vite (frontend)

| Package | Purpose |
|---------|---------|
| `react` / `react-dom` 19.2.x | UI framework |
| `typescript` 5.9 (`~5.9.3`) | Language |
| `vite` 8.x | Dev server / build |
| `vite-plugin-pwa` 1.3.x | Installable PWA, service worker generation |
| `@vitejs/plugin-react` | Vite React integration |
| `vitest` 4.x + `jsdom` | Unit testing |
| `eslint` 9 (flat config) + `typescript-eslint` 8 | Linting (`eslint.config.js`) |
| `eslint-plugin-react-hooks`, `eslint-plugin-react-refresh` | React-specific lint rules |
| `@fontsource/poppins` | Bundled font |
| `html-to-image` | Client-side image export (e.g. sharing a view) |

Node engine requirement: `^20.19.0 || >=22.12.0` (`frontend/package.json` `engines`).

## Azure / Cloud

| Service | Role |
|---------|------|
| Azurite | Local Azure Storage emulator (via Aspire `RunAsEmulator`, data volume for persistence) |
| Azure Blob Storage | Production storage for `github-cache` and `notifications` containers |
| Azure Container Apps | Deploy target (`aspire deploy`); single-replica constraint when Web Push enabled |
| Azure OAuth App (GitHub) | `GITHUB_CLIENT_ID` / `GITHUB_CLIENT_SECRET`, callback `/signin-github` |

## CI/CD

| File | Purpose |
|------|---------|
| `.github/workflows/ci.yml` | Aspire CLI install, `aspire doctor`, frontend lint/test, `dotnet restore/build/test` on `pr-timeline-app.slnx` |
| `.github/workflows/deploy.yml` | Deployment workflow (not modified by orche init) |

Both workflow files are out of scope for orche-managed changes.
