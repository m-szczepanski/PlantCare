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
GET    /api/plants/{id}/health-checks   monthly checkup history (newest first)
POST   /api/plants/{id}/health-checks   answer a checkup {status: Sick|Bad|Good|Excellent, note?} — returns the plant with rescheduled due info
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

GET    /api/reference-data/soil-types   pickable soil types with their watering-permeability factors (drives the plant-form picker and live preview)
GET    /api/reference-data/soil-mixes   DB-backed soil mix catalog (seeded from Seed/soil-mixes.json) powering the plant-form soil-mix picker
```

Profile data: `DiagnosisChecklist` is a validated JSON array (`[{symptom, causes[]}]`, `DiagnosisChecklist.TryValidate`) served inside `PlantProfile` responses and the embedded care tips; `ToxicToPets`/`ToxicToChildren` flags surface on cards, the detail page and the watering digest. `SeedLoader` also scans `SEED_CUSTOM_PATH` (default `/data/seed-custom`, on the plant-data volume) at startup for drop-in `*.json` user species — existing names skipped, malformed files logged and ignored. (single source of truth). ICS output uses CRLF line endings with 74-char folding and text escaping; adherence counts logs in the last 30 days against `window / interval` expectations; a streak is consecutive recent waterings whose gaps stay within `interval + 2` days.

## Data Model

- **`PlantProfile`** — species-level defaults (common/scientific name, `DefaultWateringIntervalDays`, `LightRequirement` enum Low/Medium/Bright/DirectSun, humidity notes, care tips text/markdown). Seeded from JSON in `Seed/`.
- **`PlantProfileTranslation`** — per-language localized profile content (`Language` two-letter code, nullable `CommonName`/`HumidityNotes`/`CareTips`/`DiagnosisChecklist`), FK-cascaded with a unique `(PlantProfileId, Language)` index. The English row on `PlantProfile` stays canonical; reads fall back field-by-field (`ProfileTranslations`). Shipped languages come from `Seed/plant-profiles.<lang>.json` (keyed by the English `commonName`, upserted at startup); PUT `/api/plant-profiles/{id}` in a non-English request language upserts that language's translation row (structural fields stay on the canonical row).
- **`Room`** — a physical room: `Name` (unique), optional `Orientation` (North/East/South/West) and environment params (`LightExposure` reusing the `LightRequirement` scale, `Humidity` Low/Medium/High, `TemperatureCelsius`). Replaced the old free-text `Plant.Location` (migration copies distinct values into rooms and rewires the FK).
- **`SoilMix`** — substrate-mix catalog (`Id`, unique `Name`) powering the plant-form soil-mix picker, seeded idempotently by name from `Seed/soil-mixes.json` (`SeedLoader.LoadSoilMixesAsync`, common houseplant recipes). Served via `GET /api/reference-data/soil-mixes`. A plant's `SoilMix` column stays a plain **name string** (not an FK) on purpose: legacy free-text values and import documents remain valid, and the picker renders any stored value not in the catalog as an extra option so edits never lose data.
- **`CareTask`** — one recurring activity per plant+type (`Watering`, `Fertilizing`, `Repotting`): `IntervalDays` (null = profile default for watering), `LastDoneAt`, `ReduceInWinter` (null = profile's `DefaultReduceInWinter`). `CareTaskLog` rows record completions (note + watering-only `AmountMilliliters`/`Method`). This replaced the old `WateringLog` table and the `CustomWateringIntervalDays`/`LastWateredAt` columns (data migrated in `AddCareTasks`); the `/water` and `/watering-logs` endpoints kept their contracts via the task layer.
- **`PlantNote`** — dated free-text health note per plant (list + create endpoints; cascade-deleted with the plant).
- **`PlantHealthCheck`** — one immutable monthly checkup answer per plant (`CheckedAt`, `Status` enum `Sick/Bad/Good/Excellent` stored as text, optional `Note`; newest-first history endpoint, no edit/delete). The current answer is mirrored on the plant (`HealthStatus`, `LastCheckupAt`) so schedule/dashboard/digest reads never touch the history table. `HealthPolicy` is the single source of truth for the health factors and the 30-day checkup period: watering intervals scale by status (sick ×1.5, bad ×1.25, good ×1, excellent ×0.8) after the soil factor and before the winter doubling; fertilizing is shortened to ×0.8 for excellent plants and *paused* while sick/bad (the due date clamps out to the next checkup day, the soil-wet pattern, with a matching `hint.fertilizingPaused` care-task hint). `Plant.CheckupReminderSentAt` stamps the per-plant ntfy reminder dedup. Plant/care-task/dashboard responses carry `healthStatus`, `lastCheckupAt` and `checkupDue` (true 30 days after acquisition and whenever the last answer goes stale); export/import include the current status and the checkup history.
- **`JournalEntry`** — growth-journal entry per plant: date + optional photo (managed upload) + optional note; feeds the detail-page before/after comparison. Photo files are cleaned up on entry delete and cascade with the plant.
- **`Plant`** — the user's owned instance. Optional FK to `PlantProfile`; `CustomWateringIntervalDays` (nullable) overrides the profile default; tracks `RoomId` (FK, SetNull), `PhotoUrl`, `AcquiredDate`, plus repot-lifecycle fields (`PotSizeCm`, `SoilType`, `SoilMix`, `PropagatedFrom`). `SoilType` (nullable enum `AllPurpose`/`CactusMix`/`ChunkyBark`/`PeatCoco`/`SemiHydro`/`SelfWatering`, stored as text) has a water-permeability factor that scales the effective watering interval — fast-draining mixes dry sooner (factor < 1, water earlier), retentive/reservoir mixes hold water (factor > 1, water later); see `SoilTypes` and `WateringScheduleService`. `PlantService` also exposes `RoomLightMatch` on the response: profile `LightRequirement` vs the room's `LightExposure` (`Good` / `SlightlyToo*` / `MuchToo*` — the "wrong room" flag at ≥2 levels), null when either side is unknown. `SoilWetUntil` (nullable instant) records a "soil is still wet" deferral: while it's in the future, `WateringScheduleService` pushes the watering due date out to that day (never earlier), so the plant leaves the overdue/due buckets (and the digest) and resurfaces on its own when the deferral lapses; watering the plant clears it. Set/cleared via `POST`/`DELETE /api/plants/{id}/soil-wet`. The plant also mirrors its current health state: `HealthStatus` (nullable enum `Sick/Bad/Good/Excellent`, stored as text) and `LastCheckupAt` — written from the newest `PlantHealthCheck` by the health-check endpoint — plus `CheckupReminderSentAt`, the notification-only stamp for the monthly digest reminder; see `PlantHealthCheck`/`HealthPolicy` for the schedule factors and the `checkupDue` rule.
- **`NotificationLog`** — audit/dedup for sent notifications (`SentAt`, `Type`), preventing duplicate sends on the same day.

### Persistence Approach

- **EF Core + SQLite** by default; the SQLite file lives on a Docker volume (`/data`). Postgres is reachable later via a provider/connection-string change only — no query rewrites expected.
- Every schema change ships with an EF Core migration, committed alongside the code change that requires it.
- Seed data is loaded from JSON at startup so users can extend species defaults without touching code.

## Localization

The API speaks two languages — English (canonical/default) and Polish (`pl`). Server-generated text (due messages in plant/care-task DTOs, the watering digest, care-task hints, ICS summaries, quick-action responses, ProblemDetails titles/details) comes from a static message catalog (`Services/Localization/Messages.cs`) behind the scoped `IAppLocalizer`. The language for HTTP requests is resolved from the caller's `Accept-Language` header (the SPA sends its UI language explicitly; unsupported values fall back to English). Background jobs have no request context and use the `APP_LANGUAGE` env default. Plurals use per-language cardinal categories (`one/few/many/other` — Polish needs all three for integers). Enum values keep crossing the wire as stable PascalCase names — the frontend maps them to localized labels; `JobRunLog.Outcome` stays a machine token for the same reason. Profile content localization is per-row (`PlantProfileTranslation`, see Data Model).

## Background Scheduling & Notifications

Handled with **Coravel** — `IInvocable` jobs run on Coravel's hosted scheduler, deliberately chosen over Hangfire/Quartz since a single daily job needs no job-store infrastructure. The scheduled job type must be registered in DI (Coravel resolves invocables via `GetRequiredService`).

Daily check flow (one digest per day, not one message per plant):

1. Coravel fires at the cron from `WATERING_CHECK_CRON` (default `0 8 * * *`).
2. Collect due/overdue plants (task interval: `IntervalDays ?? profile default`, soil-permeability scaled, health-status scaled (`HealthPolicy`), winter-doubled when reduced); shared logic lives in `WateringScheduleService`. Plants with `NotifyEnabled = false` (per-plant mute) or an active `SnoozedUntil` (vacation, set via `/api/plants/{id}/snooze` or `/api/plants/snooze-all`) are skipped. An active `SoilWetUntil` deferral (`/api/plants/{id}/soil-wet`) needs no explicit skip: it moves the watering due date out via `WateringScheduleService`, so the plant is no longer in the due/overdue set until the recheck day.
3. Also collect the monthly **health-checkup reminders**: `checkupDue` plants (30+ days past acquisition or their last answer) not yet nudged within the period (`CheckupReminderSentAt`). They ride the same digest as a trailing "Health checkups due (N):" section, or — when nothing needs water — form their own digest ("N health checkups are due"). The per-plant stamp is only written once a channel delivers, so delivery failures re-try next run; muted and snoozed plants are excluded.
4. Publish ONE digest message (`"N plants need water"`, bulleted lines, `1 plant needs water (1 overdue)` style) through every registered `INotificationChannel`. Priority 5 when anything is overdue, else 3 — delivered to ntfy always, and to Telegram when `TELEGRAM_BOT_TOKEN` + `TELEGRAM_CHAT_ID` are set (email stays unimplemented pending an SMTP decision).
5. Write a `NotificationDigest` row so a second run the same day sends nothing; the digest is only recorded if at least one channel delivered (same-day retry after a total failure).
6. Channel failure must never crash the run — each channel is try/caught; the old per-plant `NotificationLog` table was replaced by the digest log.
7. Quick actions: when `QUICK_ACTION_SECRET` is set, a digest for exactly one due plant carries an ntfy action button POSTing to `/api/plants/{id}/quick-water?key=SECRET` (constant-time compare, in-process sliding-window rate limit of 10/plant+IP per 10 min, every attempt logged). Disabled without the secret (404).

## Configuration (env-driven)

All runtime knobs come from environment variables / `.env`:

| Variable | Purpose |
|----------|---------|
| `WATERING_CHECK_CRON` | Watering-check cron expression (default `0 8 * * *`) |
| `APP_LANGUAGE` | Default language (`en`/`pl`) for server text without a request context (digests); HTTP responses follow `Accept-Language` |
| `NTFY_URL` / `NTFY_TOPIC` | Where notification POSTs go (defaults `http://ntfy:80`, `plant-care`) |
| `TELEGRAM_BOT_TOKEN` / `TELEGRAM_CHAT_ID` | Set both to also deliver digests via Telegram |
| `QUICK_ACTION_SECRET` / `QUICK_ACTION_URL_BASE` | Shared secret for quick-water buttons / public origin used to build the links |
| `ConnectionStrings__Default` | SQLite path; with the postgres profile use `Host=db;Database=plantcare;Username=plantcare;Password=…` |
| `DB_PROVIDER` | `postgres` forces the Npgsql provider (otherwise inferred from a `Host=` connection string); default SQLite |
| `POSTGRES_PASSWORD` | Password for the optional `db` service under the `postgres` compose profile |
| `PHOTO_STORAGE_PATH` | Directory for uploaded plant photos (default `/data/uploads`, the `plant-photos` volume; override for bare local runs) |
| `ENABLE_CARE_TIPS` | Set to `false` to hide the care tips section on plant detail (default on) |

### Photo storage

`PlantPhotoStorage` (Services) persists uploads under `{PHOTO_STORAGE_PATH}/plants/{plantId}/{guid}{ext}` — jpeg/png/webp/gif only, max 5 MB — and returns the public URL `/uploads/plants/...`. nginx serves that prefix from the same volume (see `docs/architecture.md` decision 7). Replacing a photo deletes the previous managed file best-effort; deleting a plant removes its upload directory.

## Conventions

- Standard .NET naming (PascalCase for types/members).
- Controllers stay thin; business logic belongs in `Services/`.
- DTOs are separate from EF Core entities — never expose entities directly.
- Tests live in `PlantCare.Api.Tests/` (unit + integration).
