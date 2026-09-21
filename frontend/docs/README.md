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
- `PlantPhoto` renders the profile `PhotoUrl` (lazy, `alt` = nickname) on cards and detail, swapping in a dashed placeholder illustration when there is no photo or the image fails to load (`onError` fallback). Uploaded photos live under `/uploads/...` (served by nginx from the shared `plant-photos` volume); the detail page's "Upload photo" button posts multipart to `POST /api/plants/{id}/photo` via `useUploadPlantPhoto`. The URL field in the plant form remains for externally hosted photos. On the detail page the photo sits to the **right** of the Details card on `lg+` and **stretches to exactly the card's height** (`lg:aspect-auto lg:self-stretch` with `object-cover` cropping — same vertical size at any content length); stacked on top with a `3/4` portrait ratio on mobile.
- `PhotoPicker` (`src/components/PhotoPicker.tsx`) is the shared file-picker block in `PlantForm` (add **and** edit; the setup wizard's plant step inherits it): the button opens the OS picker (`accept` limited to jpeg/png/webp/gif — camera on phones), a thumbnail previews the selection via `URL.createObjectURL` (revoked on change), "Remove" clears it, and >5 MB / wrong-type files are rejected inline (`role="alert"`) before any request. `PlantForm.onSubmit(input, photoFile)` hands the file to the caller; `PlantFormPage` and the wizard save the plant first, then upload through `useUploadPlantPhotoToId` (the endpoint is per-plant, so the upload rides on the saved id; the hook owns invalidation + toasts). The external-URL input stays below as a fallback — a picked file replaces it server-side.
- `PlantCard` layout: photo, name/room/species, `DueStatusBadge` (short status **text + icon** — accessible without relying on color) with a relative "Watered 3 days ago" line (`src/lib/dates.ts`), and `DueCount` — days-until-due as the primary number ("3 / days overdue" red, "Now / due today", "4 / days to go", "— / not scheduled"). When `soilWetUntil` is active it also shows a small "Soil wet — recheck {date}" note (no extra prop; derived from the plant).
- Profile care tips render via `react-markdown` (markdown text from `PlantProfile.CareTips`); no raw-HTML plugin is enabled, so profile text is XSS-safe. Output is styled with `@tailwindcss/typography` (`prose prose-sm prose-neutral dark:prose-invert max-w-none` on the care-notes block in `CareTipsCard`) — headings, lists, links and quotes all theme-aware. A TOC for very long tips is not built (seed tips are short; revisit with `feature/seed-expansion`).
- All base tokens (colors including `popover`, `chart`, `sidebar`, light + dark) live in `src/index.css`; layout/radius/animation theming is centralized in `tailwind.config.ts`. Restyling should never require touching component logic.
- Existing primitives: `button`, `input`, `label`, `card`, `badge`, `select`, `dropdown-menu`, `sidebar` (+ `sheet`, `separator`, `skeleton`, `tooltip` pulled in by it), `sonner`, `popover` + `command` + `dialog` (profile combobox), `alert-dialog`. Add more as features need them.

### Forms

- `PlantForm` also edits the repot-lifecycle fields (pot cm, soil type, soil mix, propagated-from) and a "Reduce watering in winter" checkbox (`reduceInWinter`, doubles the interval Dec–Feb; profiles can default it).
- Soil type picker: a `SoilType` `Select` in the plant form is populated from `GET /api/reference-data/soil-types` (`useSoilTypes`), so the permeability factors stay server-owned (single source of truth). The live next-due preview multiplies the base interval by the selected type's factor via `applySoilFactor` (`src/lib/soilTypes.ts` — rounding matches the backend's away-from-zero), and a hint line explains the adjustment (e.g. "Semi-hydroton (LECA) changes watering from 10 to 13 days").
- `PlantForm` UX: the species profile picker is a searchable combobox (`ProfileCombobox` = Popover + Command); a `role="status"` line previews the effective next watering date (custom interval wins, otherwise profile default, otherwise "no schedule"); `acquiredDate` defaults to today for new plants; the custom-interval placeholder shows the selected profile's default.
- API 400 responses with `ProblemDetails.errors` map to per-field messages via `splitApiError` (`src/lib/validation.ts`) — PascalCase keys become camelCase field names, inputs get `aria-invalid`; only non-field errors (e.g. unknown profile) render as the banner above the buttons.
- `CareTasksCard` (detail page) is the generic typed-task UI: each task row shows the due message (destructive when overdue) and the server's seasonal hint ("Winter rest…", flush reminder, "Winter: watering interval is doubled"), with "Mark done" per type, an "Add fertilizing" interval/skip-winter form and "Add repotting (yearly)". Mutations live in `useCareTaskMutations(plantId)` (`src/hooks/usePlants.ts`).
- Reminder controls: the plant form has a "Send watering reminders for this plant" checkbox (`notifyEnabled`); detail offers per-plant vacation snooze (3/7/14/30 days + resume, `useSnoozePlant`/`useUnsnoozePlant`), the dashboard has a "Snooze all" vacation control (`useSnoozeAllPlants`). Notifications themselves are a backend concern (daily digest, see `backend/docs/README.md`).
- Soil-wet deferral: the detail page has a "Soil is still wet" control (`useSetSoilWet(id)` with a 2/3/5/7-day recheck picker; when a deferral is active it shows "deferred until …" + "Check now" to clear via `useClearSoilWet(id)`), posting to `POST`/`DELETE /api/plants/{id}/soil-wet`. It pushes the watering due date out (the backend schedule clamps it), so the plant drops out of the overdue/due buckets until the recheck day; watering clears it. `PlantCard` shows a read-only "Soil wet — recheck {date}" note when `soilWetUntil` is in the future. Distinct from vacation snooze (notification-only); both are independent.
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

- `src/components/AppShell.tsx` is the single layout wrapper (used by `App.tsx`): a persistent shadcn sidebar with the brand and the main nav (Dashboard / Calendar / Insights / Rooms / Profiles / Status / Plants / Add plant), an active-item state driven by the route, a sticky top bar on mobile (sidebar trigger + brand), and the theme + language toggles. `/wall` deliberately renders outside the shell.
- The responsive "is mobile" state comes from `src/hooks/use-mobile.tsx` (shadcn's `useIsMobile`).

### First-Run Setup

- `SetupGate` (`src/components/SetupGate.tsx`) wraps the shell in `App.tsx`: on load it reads the shared `["plants"]`/`["rooms"]` queries; when **both lists are empty** the app is treated as launching for the first time and `SetupWizard` replaces the shell (API failures fall through to the normal app). The decision is latched per page load so the wizard never unmounts mid-flow; on finish the gate invalidates all queries and renders the shell. Rooms/plants are never auto-created anywhere (the startup seed is the species profile catalog only), so a fresh database is exactly what triggers the wizard.
- `SetupWizard` (`src/components/SetupWizard.tsx`) is a five-step card flow: Welcome → Rooms → Plants → Theme & language → All set. Rooms step reuses `useCreateRoom`/`useDeleteRoom` (name only; orientation/environment stay on `/rooms`); Plants step embeds the full `PlantForm` (remounted via `key` after each successful create so several plants can be added in a row); the preferences step writes through the existing `useTheme().setTheme` and `setLanguage()` (same `localStorage` keys as the shell toggles). Rooms/Plants steps can be skipped; the finish step summarizes the counts. All strings live under the `setup.*` i18n keys (`en`/`pl`), with Polish plural forms for the "N plants added" line.


### Internationalization (i18n)

- `i18next` + `react-i18next` are initialized in `src/i18n/index.ts` with bundled `en`/`pl` catalogs (`src/i18n/locales/*.json`); `src/main.tsx` imports it once. Components use `useTranslation()`, non-React helpers (`src/lib/*`, mutation hooks) use `i18n.t` directly.
- The language preference persists in `localStorage` under `ui-language` (same pattern as `ui-theme`) and defaults to the browser language (`pl` when it starts with `pl`). Changing it sets `<html lang>`, re-renders all text, and triggers a full query invalidation in `AppShell` so server-generated strings (due messages, localized profile content) refetch.
- `src/components/LanguageToggle.tsx` is the dropdown control (English/Polski), sitting next to the `ModeToggle` in the app shell's top bar.
- The API client (`src/api/headers.ts`) sends `Accept-Language` with the active UI language on every request (including multipart uploads), so the backend localizes `dueMessage`, hints and ProblemDetails accordingly.
- Plurals use i18next's `_one/_few/_many/_other` suffixes resolved via `Intl.PluralRules` — Polish forms (1 dzień / 2–4 dni / 5+ dni) need all three; the `count` option drives the choice.
- Date/number formatting and `localeCompare` sorting pass `i18n.language` to `toLocale*`/collation, so Polish month names and diacritic ordering follow the UI language.
- Tests run in English (`src/test/setup.ts` pins `i18n.changeLanguage("en")`); `LanguageToggle.test.tsx` covers the switch itself.

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
| Plant detail | Header with the name + due badge and the action buttons directly beneath ("Mark as watered", upload/change photo, Edit, Back to list, Delete). Below it a two-column hero: the Details card (room + light-match badge, species, acquired, last watered, pot size, soil type, soil mix, propagated-from, watering interval, next due, reminders, vacation snooze, soil-wet deferral) on the left and the photo on the right, stretched to the card's full height with `object-cover` cropping (stacked above the details as a 3/4 portrait on mobile). Then (full width) toxicity badges, care tips, diagnostics checklist, "log a watering" form (note + optional amount/method), care-tasks card, care journal, notes, and the watering history with monthly bar chart |
| Profiles | Species profile management (`/profiles`): list with plant counts/toxicity badges, create/edit form incl. care tips markdown, toxicity checkboxes and the diagnostics checklist JSON |
| Plant form | Create/edit plants; species profiles are managed via the API only (no profile form UI yet) |
| Wall mode (`/wall`) | Read-only auto-refreshing (60s) full-screen route for a home tablet — no shell/nav chrome, big cards + stats strip. Not in the nav; bookmark the URL. Calendar feed for phones: `/calendar.ics` (nginx proxies to `GET /api/calendar.ics`) |

## API Surface Used

All endpoints under `/api/*` — plants CRUD, rooms CRUD (`GET/POST/PUT/DELETE /api/rooms`), `POST /api/plants/{id}/water`, `POST /api/plants/{id}/photo` (multipart upload), `GET /api/plants/{id}/watering-logs` (detail page history), `POST`/`DELETE /api/plants/{id}/soil-wet` (watering deferral), plant-profiles list/create/update, the dashboard summary, and reference data (`GET /api/reference-data/soil-types` for the plant-form soil picker). Shapes are defined by the backend's DTOs (see `backend/docs/README.md`). Uploaded photos are static files served by nginx under `/uploads/*`, outside the API surface.

## Conventions

- Functional components only.
- Server state via hooks (TanStack Query); local UI state stays in the component.
- `useWaterPlant` is optimistic: `onMutate` patches the cached plant (list + detail) with the computed "just watered" state, failures roll the cache back, and the success toast carries an **Undo** action calling `DELETE /api/plants/{id}/water`. Destructive actions (delete plant) go through a shadcn `alert-dialog` confirm, never `window.confirm`.
- Compose shadcn primitives into app-specific components rather than editing generated files.
- Keep design tokens/theming centralized so the template is easy to re-theme.
