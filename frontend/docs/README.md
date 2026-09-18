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

- shadcn components are **composed, not modified in place** — extend via wrapper components when customization is needed.
- Theming is centralized in `tailwind.config.ts` so restyling never requires touching component logic. This is a core customization lever since the app is meant as a template others adapt.

### Server State & Data Fetching

- Typed API client helpers live in `src/api/` — all HTTP access goes through them; components never call `fetch` directly.
- Custom hooks in `src/hooks/` wrap the API client for component consumption (TanStack Query is the recommended fit for server state: caching, invalidation after mutations like "mark as watered").
- Pages in `src/pages/` compose hooks + components; no data logic in JSX.

## Key Views (v1)

| View | Contents |
|------|----------|
| Dashboard | "due today / overdue / upcoming" summary (from `GET /api/dashboard`) |
| Plant list | Owned plants with due status |
| Plant detail | Plant info, care tips from its `PlantProfile`, "mark as watered" action |
| Plant/profile forms | Create/edit plants and species profiles |

## API Surface Used

All endpoints under `/api/*` — plants CRUD, `POST /api/plants/{id}/water`, plant-profiles list/create/update, and the dashboard summary. Shapes are defined by the backend's DTOs (see `backend/docs/README.md`).

## Conventions

- Functional components only.
- Server state via hooks (TanStack Query); local UI state stays in the component.
- Compose shadcn primitives into app-specific components rather than editing generated files.
- Keep design tokens/theming centralized so the template is easy to re-theme.
