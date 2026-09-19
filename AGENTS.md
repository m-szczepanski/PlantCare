# AGENTS.md — PlantCare

Instructions for AI agents (and humans) working in this repo. Read this before making changes.

## Project

Self-hosted plant tracking web app: ASP.NET Core (.NET 10) API + React/Vite/TS SPA, EF Core + SQLite, Coravel background job, ntfy push notifications, deployed via Docker Compose. Single-user, no auth in v1.

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
2. Post-v1 work follows `docs/improvement-plan.md`: **one branch/PR per epic** (`epic/<name>` off main). Within the branch, each deliverable chunk gets its own scoped commits, in chunk order. (The historical per-chunk `feature/*` branches for Epics 1–3's early chunks were merged individually and stay as-is.)
3. Commit as you go: within a step, commit changes incrementally, grouped by feature/functionality rather than one giant commit at the end. Use short, descriptive commit messages (repo style: lowercase `type: message`, e.g. `feat: add health endpoint`).
4. Every schema change ships with an EF Core migration committed alongside the code.
5. Update docs when a change deviates from a documented decision.

## Before Implementing (every step)

- Read through the documentation first — both project docs (`docs/overview.md`, `docs/architecture.md`) and technical docs (`backend/docs/README.md`, `frontend/docs/README.md`) — plus the current step in `docs/implementation-plan.md`. Never start coding without this.

## After Implementing (every step)

- Check the acceptance criteria for the current step in `docs/implementation-plan.md`. All of them must be met. The only valid reason to skip one is if the implementation had to be done differently from what the task says (something changed in the meantime and we adjusted to it) — in that case, note the deviation.
- All projects must build after the step is done: `dotnet build backend/PlantCare.Api` and `npm run build` in `frontend/` must succeed. A step is never finished with a broken build.
- Always update `docs/implementation-plan.md` after implementation: tick off met acceptance criteria and adjust the remaining steps if reality diverged from the plan.

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
npx shadcn@latest add <component>   # in frontend/ — pull in a shadcn/ui component

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
