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
- `PlantPhoto` renders the profile `PhotoUrl` (lazy, `alt` = nickname) on cards and detail, swapping in a dashed placeholder illustration when there is no photo or the image fails to load (`onError` fallback). Uploads come later with `feature/photo-upload`.
- Profile care tips render via `react-markdown` (markdown text from `PlantProfile.CareTips`); no raw-HTML plugin is enabled, so profile text is XSS-safe.
- All base tokens (colors including `popover`, `chart`, `sidebar`, light + dark) live in `src/index.css`; layout/radius/animation theming is centralized in `tailwind.config.ts`. Restyling should never require touching component logic.
- Existing primitives: `button`, `input`, `label`, `card`, `badge`, `select`, `dropdown-menu`, `sidebar` (+ `sheet`, `separator`, `skeleton`, `tooltip` pulled in by it), `sonner`. Add more as features need them.

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

- `src/components/AppShell.tsx` is the single layout wrapper (used by `App.tsx`): a persistent shadcn sidebar with the brand and the main nav (Dashboard / Plants / Add plant — future entries like Rooms/Settings go here), an active-item state driven by the route, a sticky top bar on mobile (sidebar trigger + brand), and the theme toggle.
- The responsive "is mobile" state comes from `src/hooks/use-mobile.tsx` (shadcn's `useIsMobile`).

### Theming

- `src/components/ThemeProvider.tsx` provides `useTheme()` with three modes: `light`, `dark`, `system`. The preference persists in `localStorage` under `ui-theme` and defaults to `system`; in system mode the app follows `prefers-color-scheme` and reacts to live changes.
- `src/components/ModeToggle.tsx` is the dropdown control for the preference; it lives in the app shell's top bar (`src/components/AppShell.tsx`).
- Dark mode is class-based (`dark` on `<html>`); a small inline script in `index.html` applies the class before first paint to avoid a theme flash.
- Tokens themselves stay centralized in `src/index.css` — the provider only toggles the class.

### Server State & Data Fetching

- Typed API client helpers live in `src/api/` — all HTTP access goes through them; components never call `fetch` directly.
- Custom hooks in `src/hooks/` wrap the API client for component consumption (TanStack Query is the recommended fit for server state: caching, invalidation after mutations like "mark as watered").
- Pages in `src/pages/` compose hooks + components; no data logic in JSX.

## Key Views (v1)

| View | Contents |
|------|----------|
| Dashboard | "due today / overdue / upcoming" summary (from `GET /api/dashboard`) |
| Plant list | Owned plants with due status |
| Plant detail | Plant info, photo (or placeholder), care tips from its `PlantProfile`, "mark as watered" action, watering history |
| Plant form | Create/edit plants; species profiles are managed via the API only (no profile form UI yet) |

## API Surface Used

All endpoints under `/api/*` — plants CRUD, `POST /api/plants/{id}/water` and `GET /api/plants/{id}/watering-logs` (detail page history), plant-profiles list/create/update, and the dashboard summary. Shapes are defined by the backend's DTOs (see `backend/docs/README.md`).

## Conventions

- Functional components only.
- Server state via hooks (TanStack Query); local UI state stays in the component.
- Compose shadcn primitives into app-specific components rather than editing generated files.
- Keep design tokens/theming centralized so the template is easy to re-theme.
