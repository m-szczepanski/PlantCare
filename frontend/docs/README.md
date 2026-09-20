# Frontend Documentation

React + Vite + TypeScript single-page app — the `frontend/` directory. Consumes the backend REST API and is served by nginx in the Docker Compose `web` service.

> See `../../docs/architecture.md` for the system-level picture.

## Project Layout

```
frontend/
├── src/
│   ├── components/     # shadcn/ui-based components
│   ├── pages/          # route views (React Router paths)
│   ├── api/            # typed API client helpers
│   ├── hooks/          # custom React hooks for data fetching
│   └── lib/            # utility libraries, config
├── public/             # static assets
├── index.html
├── vite.config.ts
└── Dockerfile          # web service image (serves via nginx)
```

## Approach

### SPA, not SSR

No Next.js / server-side rendering. This is a personal, self-hosted app on a local network — a static SPA behind nginx is the simplest deployment that still gives a full app experience.

### UI Kit: shadcn/ui + Tailwind CSS

shadcn/ui is the single source for all visuals in this app. The project is registered via
`frontend/components.json`, so any component can be pulled in with:

```bash
npx shadcn@latest add <component>   # e.g. dialog, select, table, sonner
```

This drops the canonical component source into `src/components/ui/` and installs its Radix /
cva / lucide dependencies. See `components.json` for the active preset (`new-york` style,
`cssVariables`, green brand accent, `@/` path alias).

- shadcn components are **composed, not modified in place** — extend via wrapper components (e.g. `src/components/DueStatusBadge.tsx`, `src/components/PlantForm.tsx`, `src/components/PlantCard.tsx`, `src/components/PlantPhoto.tsx`, `src/components/CareTipsCard.tsx`) when customization is needed.
- `PlantPhoto` renders the profile `PhotoUrl` (lazy, `alt` = nickname) on cards and detail, swapping in a dashed placeholder illustration when there is no photo or the image fails to load (`onError` fallback). Uploaded photos live under `/uploads/...` (served by nginx from the shared `plant-photos` volume); the detail page's "Upload photo" button posts multipart to `POST /api/plants/{id}/photo` via `useUploadPlantPhoto`. The URL field in the plant form remains for externally hosted photos.
- `PlantCard` layout: photo, name/room/species, `DueStatusBadge` (short status **text + icon** — accessible without relying on color) with a relative "Watered 3 days ago" line (`src/lib/dates.ts`), and `DueCount` — days-until-due as the primary number ("3 / days overdue" red, "Now / due today", "4 / days to go", "— / not scheduled").
- Profile care tips render via `react-markdown` (markdown text from `PlantProfile.CareTips`); no raw-HTML plugin is enabled, so profile text is XSS-safe. Output is styled with `@tailwindcss/typography` (`prose prose-sm prose-neutral dark:prose-invert max-w-none` on the care-notes block in `CareTipsCard`) — headings, lists, links and quotes all theme-aware. A TOC for very long tips is not built (seed tips are short; revisit with `feature/seed-expansion`).
- All base tokens (colors including `popover`, `chart`, `sidebar`, light + dark) live in `src/index.css`; layout/radius/animation theming is centralized in `tailwind.config.ts`. Restyling should never require touching component logic.
- Existing primitives: `button`, `input`, `label`, `card`, `badge`, `select`, `dropdown-menu`, `sidebar` (+ `sheet`, `separator`, `skeleton`, `tooltip` pulled in by it), `sonner`, `popover` + `command` + `dialog` (profile combobox), `alert-dialog`. Add more as features need them.

### Forms

- `PlantForm` also edits the repot-lifecycle fields (pot cm, soil mix, propagated-from) and a "Reduce watering in winter" checkbox (`reduceInWinter`, doubles the interval Dec–Feb; profiles can default it).
- `PlantForm` UX: the species profile picker is a searchable combobox (`ProfileCombobox` = Popover + Command); a `role="status"` line previews the effective next watering date (custom interval wins, otherwise profile default, otherwise "no schedule"); `acquiredDate` defaults to today for new plants; the custom-interval placeholder shows the selected profile's default.
- API 400 responses with `ProblemDetails.errors` map to per-field messages via `splitApiError` (`src/lib/validation.ts`) — PascalCase keys become camelCase field names, inputs get `aria-invalid`; only non-field errors (e.g. unknown profile) render as the banner above the buttons.
- `CareTasksCard` (detail page) is the generic typed-task UI: each task row shows the due message (destructive when overdue) and the server's seasonal hint ("Winter rest…", flush reminder, "Winter: watering interval is doubled"), with "Mark done" per type, an "Add fertilizing" interval/skip-winter form and "Add repotting (yearly)". Mutations live in `useCareTaskMutations(plantId)` (`src/hooks/usePlants.ts`).
- Watering details: `plantsApi.water(id, details?)` sends `{note?, amountMilliliters?, method?}`; history rows render amount + method when set. `useWaterPlant`'s variables carry the same shape.
- `WateringHistoryChart` renders the last 6 months of watering counts as simple div-bars (uses the `chart-1` token, no chart library) above the history list; counts are grouped by local month from the naive-UTC log timestamps, an sr-only table exposes the data, and it hides itself when the window is empty.
- jsdom needs `ResizeObserver`/`scrollIntoView` stubs (in `src/test/setup.ts`) for Popover/Command/dialog rendering in tests.

### List filtering

- Plant list query state (search text, due filter, sort key) lives in `PlantsPage`; the pure logic is `filterPlants` in `src/lib/plantFilters.ts` (client-side, single-user dataset, no mutation of the input). The search input keeps the accessible name "Search plants" (the `keyboard-shortcuts` chunk will focus it via `/`). No-results is a distinct empty state with a clear-filters action, separate from "No plants yet".
- The list state is synced to URL search params (`?q=&due=&sort=`, defaults omitted, `replace: true`) — shareable deep links and back-navigation restore filters automatically.

### Routing & Shortcuts

- `Breadcrumbs` (shadcn-less, hand-rolled) renders at the top of the plants list, plant detail (ends with the plant's nickname, `aria-current="page"`) and the plant form; ancestor crumbs are links.
- `ScrollRestoration` (mounted in `AppShell`) remembers window scroll per router `location.key` and reapplies it when returning forward/back. Plant detail URLs (`/plants/:id`) remain the deep-link unit.
- `KeyboardShortcuts` (mounted in `AppShell`): `n` → new plant, `w` → water the focused card (cards are focusable via `data-plant-card` + `tabIndex`, the button carries `data-water-button`), `/` → focus `#plant-search` (only present on the plants list). Ignored while typing in inputs and when modifier keys are held; the Water/Add buttons carry `title` hints.

### Feedback & Toasts

- All mutation hooks report success/failure through the `sonner` toast system; `<Toaster />` is mounted once in `src/main.tsx` (top-center).
- `src/lib/toast.ts` centralizes the rules: `toastError(title, error)` derives the description from the API error (`ApiError.detail` first, so 400 validation messages surface), plus `errorMessage()` for anything that still needs inline rendering.
- Mutation hooks (`useCreatePlant`, `useUpdatePlant`, `useDeletePlant`, `useWaterPlant`) fire the toasts themselves — pages do not message on their own; query/load errors remain inline banners.
- The `ui/sonner.tsx` wrapper deviates from the generated shadcn version: it reads `resolvedTheme` from `ThemeProvider` (this repo has no next-themes). If it is ever re-added via the CLI, that wiring must be restored.

### Loading & Empty States

- Loading uses card-shaped skeletons, never plain text: `PlantCardSkeleton`/`PlantCardSkeletonGrid` for the dashboard and plants list, `PlantDetailSkeleton` for the detail page. Each exposes a `role="status"` container with an sr-only "Loading…" label and `aria-hidden` placeholder cards.
- Empty states share one `EmptyState` component (icon + title + description + optional action). Consistent usages: `NoPlantsEmptyState` (dashboard + plants list), the "No waterings logged yet" block on the detail page, and the "Plant not found" 404 state (`ApiError.status === 404` on the detail page).
- Query/load **errors** stay inline destructive banners (a 404 on the detail page is treated as not-found, not an error); mutation failures go to toasts.

### PWA / Installability

- `public/manifest.webmanifest` declares the standalone app (name, `start_url: /`, `theme_color #16a34a`) with 192/512 + maskable icons; `index.html` links it plus favicon (SVG with PNG fallback), `apple-touch-icon`, theme-color, and the mobile-web-app meta tags.
- Icons are generated deterministically from `scripts/generate-icons.mjs` (pure Node, no image deps): `node scripts/generate-icons.mjs` regenerates everything under `public/icons/`; `public/favicon.svg` is the same leaf mark in vector form.
- `nginx.conf` serves `.webmanifest` as `application/manifest+json` (nginx's default mime types don't know the extension).
- No service worker in v1 — install works without one; offline caching stays out of scope (see project non-goals).

### App Shell

- `src/components/AppShell.tsx` is the single layout wrapper (used by `App.tsx`): a persistent shadcn sidebar with the brand and the main nav (Dashboard / Calendar / Insights / Plants / Add plant — future entries like Rooms/Settings go here), an active-item state driven by the route, a sticky top bar on mobile (sidebar trigger + brand), and the theme toggle. `/wall` deliberately renders outside the shell.
- The responsive "is mobile" state comes from `src/hooks/use-mobile.tsx` (shadcn's `useIsMobile`).

### Theming

- `src/components/ThemeProvider.tsx` provides `useTheme()` with three modes: `light`, `dark`, `system`. The preference persists in `localStorage` under `ui-theme` and defaults to `system`; in system mode the app follows `prefers-color-scheme` and reacts to live changes.
- `src/components/ModeToggle.tsx` is the dropdown control for the preference; it lives in the app shell's top bar (`src/components/AppShell.tsx`).
- Dark mode is class-based (`dark` on `<html>`); a small inline script in `index.html` applies the class before first paint to avoid a theme flash.
- Tokens themselves stay centralized in `src/index.css` — the provider only toggles the class.

### Mobile

- `src/lib/ui.ts` holds the shared responsive classes: `touchButton` (≥44px targets below `sm`), `touchField` (h-11 + `text-base` on phones to stop iOS focus-zoom, compact from `sm`), `touchIcon`.
- Audit (phone width ~360–390px): dashboard/list/detail cards are single-column grids; headers wrap (`flex-wrap`); action rows wrap; long plant names break (`break-words`); sidebar collapses to the off-canvas sheet with a 44px trigger; theme toggle stays 44px on mobile. Generated shadcn files are untouched — classes are passed in at usage sites.

### Server State & Data Fetching

- Typed API client helpers live in `src/api/` — all HTTP access goes through them; components never call `fetch` directly.
- Custom hooks in `src/hooks/` wrap the API client for component consumption (TanStack Query is the recommended fit for server state: caching, invalidation after mutations like "mark as watered").
- Pages in `src/pages/` compose hooks + components; no data logic in JSX.

## Key Views (v1)

| View | Contents |
|------|----------|
| Dashboard | Stats strip (overdue / due today / total, green "all caught up" state) + "due today / overdue / upcoming" summary, toggleable to a group-by-room view (`?group=room`) |
| Calendar | Week/month grid of upcoming care, projected client-side from each plant's `nextDueDate` + interval (`src/lib/scheduleProjection.ts`); overdue plants land on today |
| Insights | Read-only collection stats from `GET /api/insights`: totals, species diversity, most-neglected, 30-day adherence, on-time streaks, monthly watering bars |
| Plant list | Owned plants with due status, search (name/species/room), due-status filter, and sort (name / room / soonest due / recently watered) |
| Rooms | Room CRUD (`/rooms`): name, orientation, optional environment (light exposure / humidity / temperature °C), plant list per room with light-match badges |
| Plant detail | Plant info (incl. pot size / soil mix / propagated-from / acquired), photo (or placeholder), care tips, "log a watering" form (note + optional amount/method), care-tasks card (watering/fertilizing/repotting with per-type "mark done", hints, add/remove), notes section (list + add), watering history with monthly bar chart |
| Plant form | Create/edit plants; species profiles are managed via the API only (no profile form UI yet) |
| Wall mode (`/wall`) | Read-only auto-refreshing (60s) full-screen route for a home tablet — no shell/nav chrome, big cards + stats strip. Not in the nav; bookmark the URL. Calendar feed for phones: `/calendar.ics` (nginx proxies to `GET /api/calendar.ics`) |

## API Surface Used

All endpoints under `/api/*` — plants CRUD, rooms CRUD (`GET/POST/PUT/DELETE /api/rooms`), `POST /api/plants/{id}/water`, `POST /api/plants/{id}/photo` (multipart upload), `GET /api/plants/{id}/watering-logs` (detail page history), plant-profiles list/create/update, and the dashboard summary. Shapes are defined by the backend's DTOs (see `backend/docs/README.md`). Uploaded photos are static files served by nginx under `/uploads/*`, outside the API surface.

## Conventions

- Functional components only.
- Server state via hooks (TanStack Query); local UI state stays in the component.
- `useWaterPlant` is optimistic: `onMutate` patches the cached plant (list + detail) with the computed "just watered" state, failures roll the cache back, and the success toast carries an **Undo** action calling `DELETE /api/plants/{id}/water`. Destructive actions (delete plant) go through a shadcn `alert-dialog` confirm, never `window.confirm`.
- Compose shadcn primitives into app-specific components rather than editing generated files.
- Keep design tokens/theming centralized so the template is easy to re-theme.
