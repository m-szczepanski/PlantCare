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
POST   /api/plants/{id}/water      logs a watering event (note + optional `amountMilliliters`, `method`: Tap/Filtered/Rainwater) — marks the Watering care task
POST   /api/plants/{id}/photo      multipart upload (field `file`) — stores the photo, sets PhotoUrl to its /uploads/ path
DELETE /api/plants/{id}/water      undo: removes the newest watering log, rewinds LastWateredAt to the previous one (no-op without logs)
GET    /api/plants/{id}/watering-logs   watering history for a plant (newest first)
GET    /api/plants/{id}/care-tasks      typed care tasks with due info + seasonal/flush hints
POST   /api/plants/{id}/care-tasks      add a schedule {type, intervalDays, reduceInWinter}
DELETE /api/plants/{id}/care-tasks/{taskId}
POST   /api/plants/{id}/care-tasks/{type}/done   generic "mark as done"
GET    /api/plants/{id}/notes           health notes (newest first)
POST   /api/plants/{id}/notes           add a note {text}
GET    /api/plants/{id}/journal         care journal entries (newest first)
POST   /api/plants/{id}/journal         multipart {entryDate?, text?, file?} — photo stored under /uploads plants/{id}/journal/
DELETE /api/plants/{id}/journal/{entryId}

GET    /api/rooms                  list rooms (+ orientation, environment params, plant counts)
POST   /api/rooms                  create room (unique name, 409 on duplicate)
PUT    /api/rooms/{id}             update room
DELETE /api/rooms/{id}             delete room (plants keep existing, RoomId set null)

GET    /api/plant-profiles         list species/profiles
POST   /api/plant-profiles
PUT    /api/plant-profiles/{id}

GET    /api/dashboard              due today / overdue / upcoming summary
GET    /api/calendar.ics           iCalendar (RFC 5545) feed: one recurring all-day event per scheduled plant
GET    /api/insights               read-only collection stats: totals, species diversity, most-neglected, 30-day adherence, on-time streaks, 12-month watering counts
```

Profile data: `DiagnosisChecklist` is a validated JSON array (`[{symptom, causes[]}]`, `DiagnosisChecklist.TryValidate`) served inside `PlantProfile` responses and the embedded care tips; `ToxicToPets`/`ToxicToChildren` flags surface on cards, the detail page and the watering digest. `SeedLoader` also scans `SEED_CUSTOM_PATH` (default `/data/seed-custom`, on the plant-data volume) at startup for drop-in `*.json` user species — existing names skipped, malformed files logged and ignored. (single source of truth). ICS output uses CRLF line endings with 74-char folding and text escaping; adherence counts logs in the last 30 days against `window / interval` expectations; a streak is consecutive recent waterings whose gaps stay within `interval + 2` days.

## Data Model

- **`PlantProfile`** — species-level defaults (common/scientific name, `DefaultWateringIntervalDays`, `LightRequirement` enum Low/Medium/Bright/DirectSun, humidity notes, care tips text/markdown). Seeded from JSON in `Seed/`.
- **`Room`** — a physical room: `Name` (unique), optional `Orientation` (North/East/South/West) and environment params (`LightExposure` reusing the `LightRequirement` scale, `Humidity` Low/Medium/High, `TemperatureCelsius`). Replaced the old free-text `Plant.Location` (migration copies distinct values into rooms and rewires the FK).
- **`CareTask`** — one recurring activity per plant+type (`Watering`, `Fertilizing`, `Repotting`): `IntervalDays` (null = profile default for watering), `LastDoneAt`, `ReduceInWinter` (null = profile's `DefaultReduceInWinter`). `CareTaskLog` rows record completions (note + watering-only `AmountMilliliters`/`Method`). This replaced the old `WateringLog` table and the `CustomWateringIntervalDays`/`LastWateredAt` columns (data migrated in `AddCareTasks`); the `/water` and `/watering-logs` endpoints kept their contracts via the task layer.
- **`PlantNote`** — dated free-text health note per plant (list + create endpoints; cascade-deleted with the plant).
- **`JournalEntry`** — growth-journal entry per plant: date + optional photo (managed upload) + optional note; feeds the detail-page before/after comparison. Photo files are cleaned up on entry delete and cascade with the plant.
- **`Plant`** — the user's owned instance. Optional FK to `PlantProfile`; `CustomWateringIntervalDays` (nullable) overrides the profile default; tracks `RoomId` (FK, SetNull), `PhotoUrl`, `AcquiredDate`, plus repot-lifecycle fields (`PotSizeCm`, `SoilMix`, `PropagatedFrom`). `PlantService` also exposes `RoomLightMatch` on the response: profile `LightRequirement` vs the room's `LightExposure` (`Good` / `SlightlyToo*` / `MuchToo*` — the "wrong room" flag at ≥2 levels), null when either side is unknown.
- **`NotificationLog`** — audit/dedup for sent notifications (`SentAt`, `Type`), preventing duplicate sends on the same day.

### Persistence Approach

- **EF Core + SQLite** by default; the SQLite file lives on a Docker volume (`/data`). Postgres is reachable later via a provider/connection-string change only — no query rewrites expected.
- Every schema change ships with an EF Core migration, committed alongside the code change that requires it.
- Seed data is loaded from JSON at startup so users can extend species defaults without touching code.

## Background Scheduling & Notifications

Handled with **Coravel** — `IInvocable` jobs run on Coravel's hosted scheduler, deliberately chosen over Hangfire/Quartz since a single daily job needs no job-store infrastructure. The scheduled job type must be registered in DI (Coravel resolves invocables via `GetRequiredService`).

Daily check flow (one digest per day, not one message per plant):

1. Coravel fires at the cron from `WATERING_CHECK_CRON` (default `0 8 * * *`).
2. Collect due/overdue plants (task interval: `IntervalDays ?? profile default`, winter-doubled when reduced); shared logic lives in `WateringScheduleService`. Plants with `NotifyEnabled = false` (per-plant mute) or an active `SnoozedUntil` (vacation, set via `/api/plants/{id}/snooze` or `/api/plants/snooze-all`) are skipped.
3. Publish ONE digest message (`"N plants need water"`, bulleted lines, `1 plant needs water (1 overdue)` style) through every registered `INotificationChannel`. Priority 5 when anything is overdue, else 3 — delivered to ntfy always, and to Telegram when `TELEGRAM_BOT_TOKEN` + `TELEGRAM_CHAT_ID` are set (email stays unimplemented pending an SMTP decision).
4. Write a `NotificationDigest` row so a second run the same day sends nothing; the digest is only recorded if at least one channel delivered (same-day retry after a total failure).
5. Channel failure must never crash the run — each channel is try/caught; the old per-plant `NotificationLog` table was replaced by the digest log.
6. Quick actions: when `QUICK_ACTION_SECRET` is set, a digest for exactly one due plant carries an ntfy action button POSTing to `/api/plants/{id}/quick-water?key=SECRET` (constant-time compare, in-process sliding-window rate limit of 10/plant+IP per 10 min, every attempt logged). Disabled without the secret (404).

## Configuration (env-driven)

All runtime knobs come from environment variables / `.env`:

| Variable | Purpose |
|----------|---------|
| `WATERING_CHECK_CRON` | Watering-check cron expression (default `0 8 * * *`) |
| `NTFY_URL` / `NTFY_TOPIC` | Where notification POSTs go (defaults `http://ntfy:80`, `plant-care`) |
| `TELEGRAM_BOT_TOKEN` / `TELEGRAM_CHAT_ID` | Set both to also deliver digests via Telegram |
| `QUICK_ACTION_SECRET` / `QUICK_ACTION_URL_BASE` | Shared secret for quick-water buttons / public origin used to build the links |
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
