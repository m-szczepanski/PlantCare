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

- shadcn components are **composed, not modified in place** — extend via wrapper components (e.g. `src/components/DueStatusBadge.tsx`, `src/components/PlantForm.tsx`, `src/components/PlantCard.tsx`, `src/components/CareTipsCard.tsx`) when customization is needed.
- Profile care tips render via `react-markdown` (markdown text from `PlantProfile.CareTips`); no raw-HTML plugin is enabled, so profile text is XSS-safe.
- All base tokens (colors including `popover`, `chart`, `sidebar`, light + dark) live in `src/index.css`; layout/radius/animation theming is centralized in `tailwind.config.ts`. Restyling should never require touching component logic.
- Existing primitives: `button`, `input`, `label`, `card`, `badge`, `select`, `dropdown-menu`. Add more as features need them.

### Theming

- `src/components/ThemeProvider.tsx` provides `useTheme()` with three modes: `light`, `dark`, `system`. The preference persists in `localStorage` under `ui-theme` and defaults to `system`; in system mode the app follows `prefers-color-scheme` and reacts to live changes.
- `src/components/ModeToggle.tsx` is the dropdown control for the preference; it lives in the header (`src/App.tsx`).
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
| Plant detail | Plant info, care tips from its `PlantProfile`, "mark as watered" action, watering history |
| Plant form | Create/edit plants; species profiles are managed via the API only (no profile form UI yet) |

## API Surface Used

All endpoints under `/api/*` — plants CRUD, `POST /api/plants/{id}/water` and `GET /api/plants/{id}/watering-logs` (detail page history), plant-profiles list/create/update, and the dashboard summary. Shapes are defined by the backend's DTOs (see `backend/docs/README.md`).

## Conventions

- Functional components only.
- Server state via hooks (TanStack Query); local UI state stays in the component.
- Compose shadcn primitives into app-specific components rather than editing generated files.
- Keep design tokens/theming centralized so the template is easy to re-theme.
