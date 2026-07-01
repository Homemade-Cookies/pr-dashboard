# AGENTS.md — frontend

React 19.2 + TypeScript 5.9 + Vite 8 dashboard UI, built as an installable PWA
(`vite-plugin-pwa`, service worker at `src/sw.ts`). See root `AGENTS.md` for project-wide
context.

## Structure

- `src/components/dashboard/` — `AttentionBoard.tsx`, `DashboardView.tsx`,
  `DashboardFilters.tsx`, `FocusExclusionDialog.tsx`, `IssuesOverview.tsx`,
  `QueueOverview.tsx`, `ShipWeekSection.tsx`, `TileDrilldown.tsx`, and the core business
  logic in `focusQueue.ts` (focus-queue bucket assignment, exclusion reasons) with its test
  in `focusQueue.test.ts`.
- `src/components/detail/` — `DetailView.tsx`, `ActivityPanel.tsx`, `ChecksPanel.tsx`,
  `DeveloperPanel.tsx`, `MilestonePanel.tsx`, `RawActivityTimeline.tsx`, `TriagePanel.tsx`.
- `src/components/` (flat) — shared/leaf components: `AuthCard.tsx`, `GitHubAvatar.tsx`,
  `HelpTooltip.tsx`, `NotificationSettings.tsx`, `PullRequestList*.tsx`, `SignalPills.tsx`,
  `MobileNav.tsx`, `LoadingBadge.tsx`, `LoadingCardPlaceholders.tsx`, `LoadingMetric.tsx`.
- `src/utils/` — data shaping and pure logic: `models.ts` (PR/attention-bucket modeling),
  `signals.ts` (signal pill dedupe), `notifications.ts` (Web Push client logic), `format.ts`
  (date/count formatting), `http.ts`, `loadLifecycle.ts`, `routing.ts`,
  `useMediaQuery.ts`. Co-located `*.test.ts` files use vitest + jsdom.
- `src/App.tsx`, `src/main.tsx`, `src/constants.ts`, `src/types.ts` — app shell, entry point,
  shared constants (repo defaults, team member lists), and shared TS types.

## Commands

```bash
npm ci            # install
npm run dev       # vite dev server (localhost:5173 when launched via `aspire start`)
npm run build     # tsc -b && vite build
npm run lint      # eslint .
npm test          # vitest run
```

## Conventions

- ESLint 9 flat config (`eslint.config.js`) with `typescript-eslint`,
  `eslint-plugin-react-hooks`, `eslint-plugin-react-refresh`. Run `npm run lint` before
  committing frontend changes.
- Tests are co-located next to the module under test (`format.test.ts` next to `format.ts`,
  not in a separate `__tests__/` tree).
- New dashboard business logic (bucket/queue rules) belongs in `utils/` or
  `components/dashboard/focusQueue.ts`, not inline in component render bodies — match the
  existing separation between pure logic (`utils/`, `focusQueue.ts`) and presentation
  (`components/`).

## Do Not

- Don't change focus-queue bucket rules without reading the "Unresolved feedback" and
  "Notifications" sections of the root `README.md` — the exclusion-label sets in
  `focusQueue.ts` (`excludedFocusBucketLabels`, `disqualifyingFocusBucketLabels`,
  `specializedFocusBucketLabels`) encode real product policy, not incidental UI state.
- Don't add a new state-management library — the app uses local component state and
  `utils/` helpers; there's no Redux/Zustand/etc. in `package.json`.
- Don't disable the PWA plugin or remove `src/sw.ts` — Web Push depends on the installed
  service worker (see `README.md`'s Notifications section for the iOS/Safari Add-to-Home-Screen
  caveat).

## Rules to Consult

`frontend-engineering/FE-react.md`, `frontend-engineering/FE-pwa.md`,
`architecture-design/AD-naming-conventions.md` (under
`/Users/ckocheno/neldevsrc/GitHub/nelnet-nbs/orche-infrastructure/rules/`).
