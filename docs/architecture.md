# Architecture

## High-Level Shape

```
┌──────────────┐     HTTP/JSON     ┌──────────────┐        ┌─────────┐
│   frontend   │ ────────────────► │     api      │ ─────► │ SQLite  │
│ React + Vite │                   │ ASP.NET Core │        │ (EF Core│
│    (nginx)   │ ◄──────────────── │  (Kestrel)   │        │  file)  │
└──────────────┘                   └──────┬───────┘        └─────────┘
                                          │ POST (daily job)
                                          ▼
                                   ┌──────────────┐
                                   │     ntfy     │ → phone/desktop push
                                   └──────────────┘
```

All services run side by side in a single Docker Compose project on the user's machine (local network).

## Layers & Responsibilities

| Service | Responsibility |
|---------|----------------|
| `web` (frontend) | Static SPA served by nginx; talks only to the REST API |
| `api` (backend) | REST endpoints, business rules, EF Core persistence, daily watering scheduler, ntfy publishing |
| `ntfy` | Off-the-shelf self-hosted push notification relay; the API is just an HTTP client of it |

## Key Architectural Decisions

1. **Monorepo** — backend and frontend live in one repo so a single Docker Compose file, one issue tracker, and one CI pipeline cover everything. Rationale: single maintainer + AI agent, no independent release cadences.
2. **SQLite first** — simplest possible persistence for a self-hosted, single-user app; the EF Core provider is the only thing that changes if Postgres is ever needed. No DB server container required by default.
3. **Coravel for scheduling** — a daily watering check doesn't justify Hangfire/Quartz infrastructure (dashboards, job stores). An `IScheduledJob`/`BackgroundService` with a cron from env config covers the requirement.
4. **ntfy for push** — avoids building mobile push infrastructure (APNs/FCM accounts, certificates). Users subscribe to a topic in the ntfy app; the API POSTs plain HTTP messages.
5. **Data over code customization** — species defaults and care tips ship as seed JSON (`backend/PlantCare.Api/Seed/`), editable by users without recompiling. Env vars drive schedule, ntfy topic/URL, and feature flags.
6. **Thin controllers** — controllers only map HTTP ↔ DTOs; business logic (due-date computation, notification dedup) lives in `Services/`.

## Data Flow: Watering Notification

```
Coravel cron fires (WATERING_CHECK_CRON)
  → WateringCheckService queries plants due (LastWateredAt + interval <= today)
  → for each due plant, POST message to http://ntfy:80/<topic>
  → write NotificationLog row (dedup: max one per plant per day)
```

## Boundaries

- The frontend has **no direct DB access** and no server-side rendering; everything goes through `/api/*`.
- The backend has **no outbound dependencies** other than ntfy (optional — app degrades gracefully if ntfy is unreachable, notifications just don't go out).
- Schema evolution only via EF Core migrations, committed with the code that needs them.
