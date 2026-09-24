# Plant Care App

A self-hosted, customizable web app for keeping track of your plants: what you own, when each one needs care, and what it needs to thrive. Deploy it with Docker on your home server or NAS and get a push notification when something needs watering.

Built for personal use on a local network. Single-user by design, no accounts, no cloud.

## Features

- **Plant management**: name, species, location, photo (upload or external URL) and acquisition date
- **Care schedules**: watering, fertilizing and repotting tasks per plant, with defaults inherited from the species profile and overridable per plant
- **Seasonal adjustment**: optional winter reduction that doubles intervals from December to February
- **Dashboard**: an at-a-glance view of what is due today, overdue and upcoming
- **Watering history**: one-click "mark as watered" with a per-plant log
- **Push notifications**: a daily background check sends reminders through [ntfy](https://ntfy.sh)
- **Care tips**: per-species light and humidity requirements plus Markdown care notes on temperature, fertilizing and more
- **Editable species library**: profiles are seeded from a JSON file and can be extended without touching code
- **Bilingual**: English and Polish UI with a language switch, including species profiles, due messages and notifications
- **Backup and restore**: JSON export/import, also available from the in-app Status page

## Quick Start

**Requirements:** Docker with Docker Compose.

```bash
git clone m-szczepanski/PlantCare
cd plant-care-app
cp .env.example .env
docker compose up -d --build
```

| Service | URL |
|---------|-----|
| Web app | http://localhost:3000 |
| API | http://localhost:5001 |
| ntfy | http://localhost:8080 |

To receive notifications, subscribe to your topic (default `plant-care`) in the [ntfy](https://ntfy.sh) mobile or desktop app, or in the browser at http://localhost:8080, pointing it at your server.

## Configuration

All settings are read from `.env`. See [`.env.example`](.env.example) for the full list.

| Variable | Default | Description |
|----------|---------|-------------|
| `WATERING_CHECK_CRON` | `0 8 * * *` | Cron schedule for the daily due-plants check |
| `NTFY_TOPIC` | `plant-care` | ntfy topic that notifications are published to |
| `ENABLE_CARE_TIPS` | `true` | Show species care tips in the plant detail view |
| `DB_CONNECTIONSTRING` | `Data Source=/data/plantcare.db` | EF Core connection string |

Use `docker-compose.override.yml` (copy from `docker-compose.override.yml.example`) for local tweaks such as changing ports.

### Customizing species and care tips

Species defaults (watering interval, light requirement, humidity notes, care tips) live in JSON under [`backend/PlantCare.Api/Seed/`](backend/PlantCare.Api/Seed/). Edit or extend the file to add your own species. You can also create and edit profiles from the app.

### Theming

Styling is centralized in `frontend/tailwind.config.ts`, so you can restyle the app without touching component logic.

### Using PostgreSQL

SQLite is the default and needs no extra setup. To switch to PostgreSQL, change the EF Core provider and set `DB_CONNECTIONSTRING`; no application code changes are required.

## How It Works

1. A [Coravel](https://github.com/jamesmh/coravel) scheduled job runs on `WATERING_CHECK_CRON`.
2. It selects plants whose care is due today or overdue, using the plant's custom interval if set and the species default otherwise.
3. For each plant not yet notified today, it publishes a message to the configured ntfy topic and records it in the notification log, which prevents duplicate reminders.
4. A failed send is logged and skipped, so an ntfy outage never interrupts the run or blocks other plants.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend | ASP.NET Core Web API (.NET 10) |
| Data | EF Core with SQLite (PostgreSQL supported via provider change) |
| Scheduling | Coravel |
| Frontend | React, Vite, TypeScript, TanStack Query |
| UI | shadcn/ui, Tailwind CSS |
| Notifications | ntfy |
| Deployment | Docker Compose, nginx |

## Project Structure

```text
plant-care-app/
├── backend/
│   ├── PlantCare.Api/          # ASP.NET Core Web API
│   │   ├── Controllers/        # REST endpoints
│   │   ├── Models/             # EF Core entities
│   │   ├── Dtos/               # Request/response contracts
│   │   ├── Data/               # DbContext and migrations
│   │   ├── Services/           # Scheduling, notifications, care logic
│   │   └── Seed/               # Default species profiles (JSON)
│   └── PlantCare.Api.Tests/    # xUnit tests
├── frontend/                   # React + Vite SPA
│   └── src/
│       ├── components/
│       ├── pages/
│       ├── api/                # Typed API client
│       ├── hooks/              # TanStack Query hooks
│       └── lib/
├── docs/                       # Project documentation
├── docker-compose.yml
├── .env.example
└── AGENTS.md                   # Contributor and agent guidelines
```

## API Overview

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/plants` | List plants with due status |
| `POST` | `/api/plants` | Create a plant |
| `GET` | `/api/plants/{id}` | Plant detail, including care tips when available |
| `PUT` | `/api/plants/{id}` | Update a plant |
| `DELETE` | `/api/plants/{id}` | Delete a plant |
| `POST` | `/api/plants/{id}/water` | Log a watering |
| `GET` | `/api/plants/{id}/watering-logs` | Watering history, newest first |
| `GET` | `/api/plant-profiles` | List species profiles |
| `POST` | `/api/plant-profiles` | Create a profile (`409` on duplicate name) |
| `PUT` | `/api/plant-profiles/{id}` | Update a profile (`409` on duplicate name) |
| `GET` | `/api/dashboard` | Due today, overdue and upcoming summary |
| `GET` | `/api/export` | Download a JSON backup |
| `POST` | `/api/import` | Import a JSON backup |
| `GET` | `/health` | Liveness probe |

## Backups

**Export and import.** `GET /api/export` downloads a JSON snapshot of rooms, plant profiles, plants with care tasks, watering logs, notes and journal entries. The same action is available from the Status page in the app. `POST /api/import` accepts that document and merges it by natural key (room name, profile common name, plant nickname). Existing records are skipped, so importing the same backup twice is safe.

**Volumes.** Uploaded photo files are not part of the JSON snapshot. To back up everything, also archive the `plant-data` and `plant-photos` volumes:

```bash
docker compose down
docker run --rm -v plantcare-app_plant-data:/data -v "$PWD:/backup" alpine \
  tar czf /backup/plant-data-backup.tgz /data
docker run --rm -v plantcare-app_plant-photos:/data -v "$PWD:/backup" alpine \
  tar czf /backup/plant-photos-backup.tgz /data
```

Adjust the volume prefix to match your Compose project name. To restore, extract the archives into fresh volumes.

## Development

**Backend**

```bash
cd backend/PlantCare.Api
dotnet run

# tests
cd ../PlantCare.Api.Tests
dotnet test
```

**Frontend**

```bash
cd frontend
npm install
npm run dev
```

Every schema change must ship with an EF Core migration in the same commit.

## Scope

Plant Care App is intentionally small. Multi-user authentication, a native mobile app and cloud sync are out of scope. It is meant to run on your own network as a personal template you can adapt.

## Documentation

- [`docs/overview.md`](docs/overview.md): feature scope and data model
- [`docs/architecture.md`](docs/architecture.md): system design and key decisions
- [`docs/implementation-plan.md`](docs/implementation-plan.md): build order, testing strategy and acceptance criteria

## Contributing

Contributions are welcome. Keep changes small and scoped, one feature slice per pull request. See [`AGENTS.md`](AGENTS.md) for coding conventions and workflow.

- **Backend:** standard .NET naming, thin controllers, business logic in `Services/`, DTOs kept separate from EF entities
- **Frontend:** functional components, TanStack Query for server state, a typed API client, shadcn/ui components composed rather than modified in place
