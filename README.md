# Plant Care App

A self-hosted, highly customizable web app for tracking owned plants, watering reminders, and care tips. Designed to be deployed via Docker by end users as a personal/home template, not a multi-tenant SaaS.

## Core Features (v1)s

- CRUD for owned plants (name, species, location, photo, acquired date)
- Watering schedule per plant, derived from a species/profile default but overridable per plant
- Daily background check that flags plants due for watering and sends a push notification
- Care tips per species (light, humidity, temperature, fertilizing notes)
- Simple dashboard: "due today / overdue / upcoming" view

**Explicit non-goals for v1:** multi-user auth, mobile app, cloud sync. Keep it single-user, local-network friendly.

## Repository Structure

**Decision: single repo (monorepo) for backend + frontend.** For a project of this size, run by one supervisor + an AI agent, a monorepo is the right call — one Docker Compose file, one set of issues/PRs, no version drift between API and UI. Split into separate repos only if this ever becomes a multi-team or publicly distributed product with independent release cadences.

```mds
plant-care-app/
├── docker-compose.yml
├── docker-compose.override.yml.example
├── .env.example
├── README.md
├── instructions.md              # detailed project instructions
├── backend/
│   ├── PlantCare.Api/           # ASP.NET Core Web API (.NET 8/9)
│   │   ├── Controllers/         # REST endpoints
│   │   ├── Models/              # EF Core entities
│   │   ├── Dtos/                # Data transfer objects
│   │   ├── Data/                # DbContext, migrations
│   │   ├── Services/            # scheduling, notification, care-tip logic
│   │   ├── Seed/                # default species/profile seed data (JSON)
│   │   ├── Program.cs
│   │   └── PlantCare.Api.csproj
│   ├── PlantCare.Api.Tests/     # unit/integration tests
│   └── Dockerfile               # for the API service
├── frontend/                    # React + Vite SPA
│   ├── src/
│   │   ├── components/          # shadcn/ui-based components
│   │   ├── pages/               # route views (or use React Router paths)
│   │   ├── api/                 # typed API client helpers
│   │   ├── hooks/               # custom React hooks for data fetching
│   │   └── lib/                 # utility libraries, config
│   ├── public/                  # static assets
│   ├── index.html
│   ├── vite.config.ts
│   └── Dockerfile               # for the web service (serves via nginx)
└── docs/                        # optional: expand on decisions as they're made
    └── architecture.md
```

## Tech Stack

| Layer | Choice | Notes |
|-------|--------|-------|
| Backend | ASP.NET Core Web API (.NET 8/9) | REST/JSON API |
| ORM / DB | EF Core + SQLite (default) | Swappable to Postgres via provider change only |
| Scheduling | Coravel (`IScheduledJob` + `BackgroundService`) | No extra infra needed; avoid Hangfire/Quartz unless job history/UI becomes a requirement |
| Frontend | React + Vite + TypeScript | SPA, not Next.js |
| UI kit | shadcn/ui + Tailwind CSS | Consistent, accessible components |
| Notifications | ntfy (self-hosted container) | API POSTs to ntfy on due/overdue plants |
| Containerization | Docker Compose | `api`, `web`, `ntfy`, optional `postgres` |

## Data Model (initial draft)

```md
PlantProfile (species-level defaults — seedable/customizable)
- Id
- CommonName
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
- PhotoUrl (nullable)
- AcquiredDate
- CustomWateringIntervalDays (nullable — overrides profile default)
- LastWateredAt

WateringLog
- Id
- PlantId (FK)
- WateredAt
- Note (nullable)

NotificationLog (optional, for dedup/audit)
- Id
- PlantId (FK)
- SentAt
- Type (e.g., WateringDue)
```

Seed `PlantProfile` data from a JSON file in `backend/PlantCare.Api/Seed/` so users can extend/edit species defaults without touching code — this is the main "customization" lever for care tips and default schedules.

## API Endpoints (initial draft)

```mds
GET    /api/plants                 list owned plants (+ due status)
POST   /api/plants
GET    /api/plants/{id}
PUT    /api/plants/{id}
DELETE /api/plants/{id}
POST   /api/plants/{id}/water      logs a watering event, updates LastWateredAt

GET    /api/plant-profiles         list species/profiles
POST   /api/plant-profiles
PUT    /api/plant-profiles/{id}

GET    /api/dashboard              due today / overdue / upcoming summary
```

## Notification Flow

1. A `BackgroundService` (via Coravel) runs once daily (configurable via env var, e.g. `WATERING_CHECK_CRON`).
2. It queries plants where `LastWateredAt + interval <= today`.
3. For each due/overdue plant, POST a message to the `ntfy` container's topic (e.g., `http://ntfy:80/plant-care`).
4. Log the notification in `NotificationLog` to avoid duplicate sends on the same day.
5. User subscribes to the ntfy topic from their phone/desktop ntfy app or browser.

## Customization Strategy

Since this is meant to be a template others can adapt:

- **Data over code**: species defaults and care tips live in seed JSON, not hardcoded.
- **Env-driven config**: notification schedule, ntfy topic/URL, DB provider connection string, feature flags (e.g., `ENABLE_CARE_TIPS=true`) all come from `.env`.
- **Theming**: keep shadcn/Tailwind config centralized (`tailwind.config.ts`) so restyling doesn't require touching component logic.

## Docker Compose (target shape)

```yaml
services:
  api:
    build: ./backend/PlantCare.Api
    env_file: .env
    volumes:
      - plant-data:/data      # SQLite file lives here
    ports:
      - "5000:8080"

  web:
    build: ./frontend
    ports:
      - "3000:80"
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
- **Frontend**: functional components, hooks for data fetching (consider React Query/TanStack Query for server state), shadcn components composed rather than modified in place where possible.
- **Commits**: small, scoped commits; agentic AI changes should be reviewable in isolated PRs per feature slice (e.g., "add PlantProfile CRUD", "add watering scheduler").
- **Migrations**: every schema change ships with an EF Core migration, committed alongside the code change that needs it.

## Suggested Build Order (milestones)

1. Scaffold repo structure + empty Docker Compose (containers boot, health check endpoint responds).
2. EF Core models + SQLite + initial migration (`Plant`, `PlantProfile`).
3. Plant CRUD API + minimal React list/detail views.
4. Watering log endpoint + "mark as watered" UI action.
5. Dashboard endpoint (due today / overdue / upcoming) + dashboard UI.
6. Coravel background job + ntfy integration for due-plant notifications.
7. Care tips display (from `PlantProfile`) in plant detail view.
8. Seed data expansion + polish (shadcn theming, photo upload, empty states).

## Open Questions to Resolve Early

- Photo storage: local volume vs. skip photos for v1?
- Should `CustomWateringIntervalDays` support more complex rules (e.g., seasonal adjustment) later, or stay a flat interval for v1?
- Single ntfy topic for everything, or per-category topics (watering vs. future feature notifications)?
