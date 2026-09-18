# AGENTS.md — PlantCare

Instructions for AI agents (and humans) working in this repo. Read this before making changes.

## Project

Self-hosted plant tracking web app: ASP.NET Core (.NET 8/9) API + React/Vite/TS SPA, EF Core + SQLite, Coravel background job, ntfy push notifications, deployed via Docker Compose. Single-user, no auth in v1.

## Documentation Map (read the relevant one before working)

| Doc | When to read |
|-----|--------------|
| `docs/overview.md` | Feature scope, data model, non-goals |
| `docs/architecture.md` | System shape, key decisions, boundaries |
| `docs/implementation-plan.md` | **What to build next, how to test, acceptance criteria** |
| `backend/docs/README.md` | API design, EF Core, scheduling, backend conventions |
| `frontend/docs/README.md` | SPA structure, data fetching, theming, frontend conventions |

## Workflow

1. Work strictly in the order of `docs/implementation-plan.md`. Do not skip steps or start the next step before the current one's acceptance criteria pass.
2. Each step (or coherent slice) is its own scoped commit/PR, e.g. `add PlantProfile CRUD`, `add watering scheduler`.
3. Every schema change ships with an EF Core migration committed alongside the code.
4. Update docs when a change deviates from a documented decision.

## Commands

```bash
# Backend
dotnet build backend/PlantCare.Api
dotnet test backend/PlantCare.Api.Tests
dotnet ef migrations add <Name> --project backend/PlantCare.Api

# Frontend
npm run dev        # in frontend/
npm run test       # Vitest
npm run build
npm run lint       # if configured

# Full stack
docker compose up --build
```

Verify builds and tests pass before finishing any task.

## Conventions

### Backend
- PascalCase for types/members; controllers thin; business logic in `Services/`.
- DTOs separate from EF entities — never expose entities over HTTP.
- Due-date/status logic lives in exactly one service (single source of truth).
- Test new behavior in `PlantCare.Api.Tests/` (xUnit; integration tests via `WebApplicationFactory`).

### Frontend
- Functional components only; typed API client in `src/api/`, hooks in `src/hooks/` (TanStack Query), pages in `src/pages/`.
- No raw `fetch` in components.
- Compose shadcn primitives; never edit generated shadcn files in place.
- Theming centralized in `tailwind.config.ts`.

### General
- No secrets in code; all config via env vars (document new ones in `.env.example`).
- No comments unless the code truly needs one; no emojis in files.
- Keep changes minimal and focused on the task.

## Guardrails

- Non-goals (v1): multi-user auth, mobile app, cloud sync — reject scope creep toward these.
- Don't add Hangfire/Quartz, Postgres, or extra infra unless the docs' decisions change.
- ntfy unavailability must degrade gracefully, never crash the API.
- Do not edit `.opencode/` config files unless explicitly asked.
