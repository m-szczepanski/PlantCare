# Plant Care App

A self-hosted, highly customizable web app for tracking owned plants, watering reminders, and care tips. Designed to be deployed via Docker by end users as a personal/home template, not a multi-tenant SaaS.

## Core Features (v1)

- CRUD for owned plants (name, species, location, photo URL, acquired date)
- Watering schedule per plant, derived from a species/profile default but overridable per plant
- Daily background check that flags plants due for watering and sends a push notification
- Care tips per species (light, humidity notes, plus markdown care notes covering topics like temperature and fertilizing)
- Simple dashboard: "due today / overdue / upcoming" view
- "Mark as watered" action with a per-plant watering history

**Explicit non-goals for v1:** multi-user auth, mobile app, cloud sync. Keep it single-user, local-network friendly.

## Repository Structure

**Decision: single repo (monorepo) for backend + frontend.** For a project of this size, run by one supervisor + an AI agent, a monorepo is the right call — one Docker Compose file, one set of issues/PRs, no version drift between API and UI. Split into separate repos only if this ever becomes a multi-team or publicly distributed product with independent release cadences.

```text
plant-care-app/
├── docker-compose.yml
├── docker-compose.override.yml.example   # copy to docker-compose.override.yml for local tweaks
├── .env.example
├── README.md                       # this file (feature/product source of truth)
├── AGENTS.md                       # working instructions for agents and humans
├── backend/
│   ├── PlantCare.Api/              # ASP.NET Core Web API (.NET 10)
│   │   ├── Controllers/            # REST endpoints (thin)
│   │   ├── Models/                 # EF Core entities
│   │   ├── Dtos/                   # Data transfer objects
│   │   ├── Data/                   # DbContext, migrations
│   │   ├── Services/               # scheduling, notification, care-tip logic
│   │   ├── Seed/                   # default species/profile seed data (JSON)
│   │   ├── Program.cs
│   │   └── Dockerfile              # for the API service
│   ├── PlantCare.Api.Tests/        # xUnit unit/integration tests
│   └── docs/                       # backend documentation
├── frontend/                       # React + Vite SPA
│   ├── src/
│   │   ├── components/             # shadcn/ui-based components
│   │   ├── pages/                  # route views (React Router paths)
│   │   ├── api/                    # typed API client helpers
│   │   ├── hooks/                  # TanStack Query hooks for server state
│   │   └── lib/                    # utility libraries, config
│   ├── docs/                       # frontend documentation
│   ├── vite.config.ts
│   └── Dockerfile                  # for the web service (serves via nginx)
└── docs/                           # project-level docs
    ├── overview.md                 # feature scope, data model, non-goals
    ├── architecture.md             # system shape, key decisions, boundaries
    └── implementation-plan.md      # build order, test strategy, acceptance criteria
```

## Tech Stack

| Layer | Choice | Notes |
|-------|--------|-------|
| Backend | ASP.NET Core Web API (.NET 10) | REST/JSON API |
| ORM / DB | EF Core + SQLite (default) | Swappable to Postgres via provider change only |
| Scheduling | Coravel (`IInvocable` jobs on a hosted scheduler) | No extra infra needed; avoid Hangfire/Quartz unless job history/UI becomes a requirement |
| Frontend | React + Vite + TypeScript | SPA, not Next.js |
| UI kit | shadcn/ui + Tailwind CSS | Consistent, accessible components |
| Notifications | ntfy (self-hosted container) | API POSTs to ntfy on due/overdue plants |
| Containerization | Docker Compose | `api`, `web`, `ntfy`, optional `postgres` |

## Data Model

```text
PlantProfile (species-level defaults — seedable/customizable)
- Id
- CommonName (unique)
- ScientificName (nullable)
- DefaultWateringIntervalDays
- LightRequirement (enum: Low/Medium/Bright/DirectSun)
- HumidityNotes
- CareTips (text/markdown)

Plant (user's owned instance)
- Id
- PlantProfileId (FK, nullable — user can create a plant without a profile)
- NickName
- Location (e.g., "Living room window")
- PhotoUrl (nullable — external URL, no upload in v1)
- AcquiredDate
- CustomWateringIntervalDays (nullable — overrides profile default)
- LastWateredAt

WateringLog
- Id
- PlantId (FK)
- WateredAt
- Note (nullable)

NotificationLog (dedup/audit)
- Id
- PlantId (FK)
- SentAt
- Type (e.g., WateringDue)
```

Seed `PlantProfile` data from a JSON file in `backend/PlantCare.Api/Seed/` so users can extend/edit species defaults without touching code — this is the main "customization" lever for care tips and default schedules. Plant-level waterings and notification audit are written at runtime, not seeded.

## API Endpoints

```text
GET    /api/plants                        list owned plants (+ due status)
POST   /api/plants
GET    /api/plants/{id}                   detail (+ care tips when profiled and enabled)
PUT    /api/plants/{id}
DELETE /api/plants/{id}
POST   /api/plants/{id}/water             logs a watering event, updates LastWateredAt
GET    /api/plants/{id}/watering-logs     watering history for a plant (newest first)

GET    /api/plant-profiles                list species/profiles
POST   /api/plant-profiles                create a profile (409 on duplicate name)
PUT    /api/plant-profiles/{id}           update a profile (409 on duplicate name)

GET    /api/dashboard                     due today / overdue / upcoming summary

GET    /health                            liveness probe (no DB dependency)
```

## Notification Flow

1. Coravel's hosted scheduler fires on a cron from `WATERING_CHECK_CRON` (default `0 8 * * *`).
2. The watering check service selects plants whose due status is `Overdue` or `DueToday` (`LastWateredAt + interval <= today`, interval = custom override ?? profile default).
3. For each due plant not yet notified today (`NotificationLog` dedup), POST a message to the `ntfy` container's topic (e.g., `http://ntfy:80/plant-care`).
4. Record the send in `NotificationLog`. A failed send is logged and skipped — ntfy outages never crash the run or block the remaining plants.
5. User subscribes to the ntfy topic from their phone/desktop ntfy app or browser.

## Customization Strategy

Since this is meant to be a template others can adapt:

- **Data over code**: species defaults and care tips live in seed JSON, not hardcoded.
- **Env-driven config**: notification schedule, ntfy topic/URL, DB provider connection string, feature flags (e.g., `ENABLE_CARE_TIPS=true`) all come from `.env`.
- **Theming**: keep shadcn/Tailwind config centralized (`tailwind.config.ts`) so restyling doesn't require touching component logic.

## Docker Compose

```yaml
services:
  api:
    build: ./backend/PlantCare.Api
    env_file:
      - path: .env
        required: false
    environment:
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__Default: ${DB_CONNECTIONSTRING:-Data Source=/data/plantcare.db}
    volumes:
      - plant-data:/data      # SQLite file lives here
    ports:
      - "5001:8080"   # 5000 avoided: macOS AirPlay Receiver binds it

  web:
    build: ./frontend
    ports:
      - "3000:80"     # nginx serves the SPA and reverse-proxies /api to the api service
    depends_on:
      - api

  ntfy:
    image: binwiederhier/ntfy
    command: serve
    ports:
      - "8080:80"
    volumes:
      - ntfy-data:/var/lib/ntfy

volumes:
  plant-data:
  ntfy-data:
```

Add `postgres` as an optional service later if/when moving off SQLite.

## Conventions

- **Backend**: standard .NET naming (PascalCase for types/members), controllers thin, business logic in `Services/`, DTOs separate from EF entities.
- **Frontend**: functional components, TanStack Query hooks for server state, typed API client, shadcn components composed rather than modified in place.
- **Commits**: small, scoped commits; agentic AI changes should be reviewable in isolated PRs per feature slice (e.g., "add PlantProfile CRUD", "add watering scheduler").
- **Migrations**: every schema change ships with an EF Core migration, committed alongside the code change that needs it.

## Build Order (milestones)

All eight milestones are implemented; see `docs/implementation-plan.md` for the per-step acceptance criteria and deviations.

1. Repo & Docker scaffold + health endpoint — done
2. EF Core models + SQLite + initial migration — done
3. Plant CRUD API + minimal frontend views — done
4. Watering log endpoint + "mark as watered" UI action — done
5. Dashboard endpoint + dashboard UI — done
6. Coravel background job + ntfy notifications — done
7. Care tips display in plant detail view — done
8. Polish: production-readiness audit (docs sync, dead code, states) — done

## Open Questions (resolved)

- **Photo storage:** skipped uploads for v1; `PhotoUrl` accepts an externally hosted URL.
- **Watering intervals:** stay a flat per-plant override of the profile default; no seasonal rules in v1.
- **ntfy topics:** single topic (`NTFY_TOPIC`) for all notifications; revisit if a second category is ever added.
