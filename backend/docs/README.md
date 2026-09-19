# Backend Documentation

ASP.NET Core Web API (.NET 10) — the `backend/PlantCare.Api/` project. REST/JSON API backed by EF Core + SQLite, with a Coravel-scheduled daily watering check that pushes notifications through a self-hosted ntfy container.

> See `../../docs/architecture.md` for the system-level picture.

## Project Layout

```
backend/
├── PlantCare.Api/
│   ├── Controllers/    # REST endpoints (thin — HTTP ↔ DTO mapping only)
│   ├── Models/         # EF Core entities
│   ├── Dtos/           # Request/response DTOs (separate from entities)
│   ├── Data/           # DbContext, EF Core migrations
│   ├── Services/       # Business logic: scheduling, notification, care tips
│   ├── Seed/           # Default species/profile seed data (JSON)
│   ├── Program.cs      # Composition root / DI registration
│   └── Dockerfile      # API service image
└── PlantCare.Api.Tests/   # unit/integration tests
```

## API Endpoints

```
GET    /api/plants                 list owned plants (+ due status)
POST   /api/plants
GET    /api/plants/{id}
PUT    /api/plants/{id}
DELETE /api/plants/{id}
POST   /api/plants/{id}/water      logs a watering event, updates LastWateredAt
POST   /api/plants/{id}/photo      multipart upload (field `file`) — stores the photo, sets PhotoUrl to its /uploads/ path
DELETE /api/plants/{id}/water      undo: removes the newest watering log, rewinds LastWateredAt to the previous one (no-op without logs)
GET    /api/plants/{id}/watering-logs   watering history for a plant (newest first)

GET    /api/plant-profiles         list species/profiles
POST   /api/plant-profiles
PUT    /api/plant-profiles/{id}

GET    /api/dashboard              due today / overdue / upcoming summary
```

## Data Model

- **`PlantProfile`** — species-level defaults (common/scientific name, `DefaultWateringIntervalDays`, `LightRequirement` enum Low/Medium/Bright/DirectSun, humidity notes, care tips text/markdown). Seeded from JSON in `Seed/`.
- **`Plant`** — the user's owned instance. Optional FK to `PlantProfile`; `CustomWateringIntervalDays` (nullable) overrides the profile default; tracks `Location`, `PhotoUrl`, `AcquiredDate`, `LastWateredAt`.
- **`WateringLog`** — history of watering events per plant (`WateredAt`, optional note).
- **`NotificationLog`** — audit/dedup for sent notifications (`SentAt`, `Type`), preventing duplicate sends on the same day.

### Persistence Approach

- **EF Core + SQLite** by default; the SQLite file lives on a Docker volume (`/data`). Postgres is reachable later via a provider/connection-string change only — no query rewrites expected.
- Every schema change ships with an EF Core migration, committed alongside the code change that requires it.
- Seed data is loaded from JSON at startup so users can extend species defaults without touching code.

## Background Scheduling & Notifications

Handled with **Coravel** — `IInvocable` jobs run on Coravel's hosted scheduler, deliberately chosen over Hangfire/Quartz since a single daily job needs no job-store infrastructure. The scheduled job type must be registered in DI (Coravel resolves invocables via `GetRequiredService`).

Daily check flow:

1. Coravel fires at the cron from `WATERING_CHECK_CRON` (default `0 8 * * *`).
2. Query plants where `LastWateredAt + interval <= today` (interval = custom override ?? profile default); shared logic lives in `WateringScheduleService`.
3. For each due/overdue plant, POST a message to the ntfy container topic (e.g., `http://ntfy:80/plant-care`).
4. Write a `NotificationLog` row to avoid duplicate sends on the same day (at most one `WateringDue` per plant per day).
5. ntfy failure must not crash the job or block the rest of the check — degrade gracefully.

## Configuration (env-driven)

All runtime knobs come from environment variables / `.env`:

| Variable | Purpose |
|----------|---------|
| `WATERING_CHECK_CRON` | Watering-check cron expression (default `0 8 * * *`) |
| `NTFY_URL` / `NTFY_TOPIC` | Where notification POSTs go (defaults `http://ntfy:80`, `plant-care`) |
| `ConnectionStrings__Default` | SQLite path (or Postgres later) |
| `PHOTO_STORAGE_PATH` | Directory for uploaded plant photos (default `/data/uploads`, the `plant-photos` volume; override for bare local runs) |
| `ENABLE_CARE_TIPS` | Set to `false` to hide the care tips section on plant detail (default on) |

### Photo storage

`PlantPhotoStorage` (Services) persists uploads under `{PHOTO_STORAGE_PATH}/plants/{plantId}/{guid}{ext}` — jpeg/png/webp/gif only, max 5 MB — and returns the public URL `/uploads/plants/...`. nginx serves that prefix from the same volume (see `docs/architecture.md` decision 7). Replacing a photo deletes the previous managed file best-effort; deleting a plant removes its upload directory.

## Conventions

- Standard .NET naming (PascalCase for types/members).
- Controllers stay thin; business logic belongs in `Services/`.
- DTOs are separate from EF Core entities — never expose entities directly.
- Tests live in `PlantCare.Api.Tests/` (unit + integration).
