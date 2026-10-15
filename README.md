# Plant Care App

A self-hosted plant tracker: keep tabs on what you own, when each one needs care, and what it needs to thrive. Run it with Docker on your home server or NAS, open it from any device on your network, and get a push notification when something needs watering.

Built for personal use on a local network. Single-user by design - no accounts, no cloud, and your data never leaves your house.

![status](https://img.shields.io/badge/status-late%20stage%20development-yellowgreen)
![platform](https://img.shields.io/badge/platform-all-blue)
![license](https://img.shields.io/badge/license-MIT-green)

## Features

- **Plant management**: name, species, location, photo (upload or external URL) and acquisition date
- **Care schedules**: watering, fertilizing and repotting tasks per plant, with defaults inherited from the species profile and overridable per plant
- **Seasonal adjustment**: optional winter reduction that doubles intervals from December to February
- **Dashboard**: an at-a-glance view of what is due today, overdue and upcoming
- **Watering history**: one-click "mark as watered" with a per-plant log
- **Push notifications**: a daily background check sends reminders through [ntfy](https://ntfy.sh)
- **Care tips**: per-species light and humidity requirements plus Markdown care notes on temperature, fertilizing and more
- **Editable species library**: profiles ship as JSON and can be extended without touching code
- **Bilingual**: English and Polish UI with a language switch, including species profiles, due messages and notifications
- **Backup and restore**: JSON export/import, also available from the in-app Status page
- **Self-watering hardware (optional)**: link ESP32-class pumps/sensors over MQTT, see live soil moisture, water on demand, and let a daily auto-water job run guarded by moisture and per-day caps (off by default)

## Deploy on your own server

Anything that runs Docker on your home network will do: a NAS, a mini-PC, a Raspberry Pi or a spare machine. All you need is Docker with the Compose plugin.

> **Keep it on your network.** Plant Care has no login screen. Don't expose it directly to the internet - for access from outside your home, put it behind a VPN (Tailscale, WireGuard, …) or a reverse proxy that handles authentication itself.

### Option A: prebuilt images (recommended for a server)

Create a folder for the stack and save this as `docker-compose.yml` inside it:

```yaml
services:
  web:
    image: ghcr.io/m-szczepanski/plantcare-web:latest
    ports:
      - "3000:80"
    volumes:
      - plant-photos:/usr/share/nginx/html/uploads:ro
    depends_on: [api]
    restart: unless-stopped

  api:
    image: ghcr.io/m-szczepanski/plantcare-api:latest
    env_file:
      - path: .env
        required: false
    environment:
      ASPNETCORE_URLS: http://+:8080
      TZ: ${TZ:-UTC}
    volumes:
      - plant-data:/data
      - plant-photos:/data/uploads
    ports:
      - "5001:8080"
    restart: unless-stopped

  ntfy:
    image: binwiederhier/ntfy
    command: serve
    ports:
      - "${NTFY_PUBLIC_PORT:-8080}:80"
    volumes:
      - ntfy-data:/var/lib/ntfy
    restart: unless-stopped

volumes:
  plant-data:
  plant-photos:
  ntfy-data:
```

The images are published under [`m-szczepanski`](https://github.com/m-szczepanski) on the GitHub Container Registry and are public, so no login is needed to pull them.

> **Forking this repo to make it your own?** Publish your own images and change the account in both `image:` lines (e.g. `ghcr.io/<your-user>/plantcare-web:latest`). Otherwise your stack would keep pulling the upstream builds instead of the ones you changed.

Then start it:

```bash
docker compose up -d
```

The defaults work as-is. To change ports, time zone or topic, first copy [`.env.example`](.env.example) from the repository to `.env` alongside the compose file - the stack picks it up automatically.

Open `http://<server-ip>:3000`. The first launch shows a short setup wizard; after that you land on the dashboard.

Running your stacks from a NAS UI (Dockge, Portainer, Unraid, …) instead? Paste the same file there - it is an ordinary Compose stack.

| Service | URL |
| --------- | ----- |
| Web app | http://&lt;server-ip&gt;:3000 |
| ntfy (notification UI) | http://&lt;server-ip&gt;:8080 |
| API (optional, direct access) | http://&lt;server-ip&gt;:5001 |

### Option B: build from source

Best for a quick look or if you want to change the code.

```bash
git clone https://github.com/m-szczepanski/PlantCare.git
cd PlantCare
cp .env.example .env
docker compose up -d --build
```

### Notifications

Watering reminders are delivered through [ntfy](https://ntfy.sh), which ships with the stack - nothing to sign up for.

1. Install the ntfy app on your phone (or use its desktop/browser client).
2. Add your own server, pointing at your host, e.g. `http://<server-ip>:8080`.
3. Subscribe to the topic `plant-care`.
4. In PlantCare, open the **Status** page and press **Send test notification** to confirm it arrives.

If you map the `ntfy` service on a different host port, set `NTFY_PUBLIC_PORT` to match - the app uses it to build the subscribe link it shows you. You can also open the notification UI directly at `http://<server-ip>:8080/plant-care`.

### Updating to a new version

```bash
docker compose pull
docker compose up -d
```

Your data is untouched: it lives in named volumes, and any database changes are applied automatically when the app starts.

### Where your data lives

Everything is stored in named volumes - the database in `plant-data`, uploaded photos in `plant-photos`, notification history in `ntfy-data`. They survive restarts, updates and container recreation.

> Never run `docker compose down -v` unless you intend to erase all of it - that flag deletes the volumes.

## Configuration

Every setting is optional; sensible defaults are built in. Copy `.env.example` to `.env` and change what you need. The ones people usually touch:

| Setting | Default | What it does |
| --------- | --------- | -------------- |
| `TZ` | `UTC` | Time zone used for the daily check |
| `WATERING_CHECK_CRON` | `0 8 * * *` | When the daily watering check runs (08:00 by default) |
| `NTFY_TOPIC` | `plant-care` | Topic reminders are sent to |
| `NTFY_PUBLIC_PORT` | `8080` | Host port ntfy is published on (must match the `ports:` mapping) |
| `APP_LANGUAGE` | `en` | Language for notifications - `en` or `pl` |
| `ENABLE_CARE_TIPS` | `true` | Show the species care-tips section |
| `ENABLE_DEVICES` | `false` | Turn on the optional self-watering hardware |

The full list - including device, MQTT and PostgreSQL settings - is documented in [`.env.example`](.env.example). Local-only experiments can go in a `docker-compose.override.yml` (copy from `docker-compose.override.yml.example`), which Compose loads automatically.

## Self-watering devices (optional)

Off by default. If you have - or want to try - ESP32-class pumps and soil-moisture sensors, PlantCare can drive them over MQTT using the bundled Mosquitto broker.

1. **Add the broker to your stack.** Append this service to the `services:` block in your compose file (a sibling of `web` and `api`), and save the config file from [`mosquitto/mosquitto.conf`](mosquitto/mosquitto.conf) to `mosquitto/mosquitto.conf` next to that file:

   ```yaml
   mqtt:
     image: eclipse-mosquitto:2
     restart: unless-stopped
     ports:
       - "1883:1883"
     volumes:
       - mqtt-data:/mosquitto/data
       - ./mosquitto/mosquitto.conf:/mosquitto/config/mosquitto.conf:ro
   ```

   Then add one line - `mqtt-data:` - to your existing top-level `volumes:` block. Both halves are required: without the volume declaration Compose refuses to start, and the config must be a real file, not just a folder (Mosquitto 2 otherwise listens on localhost only and the app could never reach it).

2. **Set `ENABLE_DEVICES=true`** in `.env` and run `docker compose up -d`.
3. **Add the device** on the **Devices** page - the app assigns it an id.
4. **Flash the matching firmware** from [`docs/firmware/`](docs/firmware/README.md) (ESPHome by default, or a Tasmota relay), pointed at your broker and that id.
5. **Link it to a plant.** You then get live soil moisture, a **Water now** button, and automatic watering for due plants - guarded by a moisture threshold, a duration cap and a per-day cap.

No hardware yet? [`tools/stub-device.py`](tools/stub-device.py) stands in for a device so you can watch the whole loop work end to end.

The full topic schema, settings and safety rules are in [`docs/hardware-integration.md`](docs/hardware-integration.md).

## Backups

**Export and import.** The **Status** page has buttons to download and restore a JSON snapshot of your rooms, species profiles, plants, care tasks, watering logs, notes and journal entries. Restoring merges by name, so importing the same backup twice is safe.

**Photos.** Uploaded images are not part of that snapshot. To back up everything, also archive the volumes (this briefly stops the stack):

```bash
docker compose down
docker run --rm -v plantcare_plant-data:/data -v "$PWD:/backup" alpine \
  tar czf /backup/plant-data-backup.tgz /data
docker run --rm -v plantcare_plant-photos:/data -v "$PWD:/backup" alpine \
  tar czf /backup/plant-photos-backup.tgz /data
```

Compose prefixes volume names with the project name (the folder name, here `plantcare`) - adjust if yours differs. To restore, extract the archives into fresh volumes.

## Customizing it

**Species and care tips.** Profiles ship as JSON ([`backend/PlantCare.Api/Seed/`](backend/PlantCare.Api/Seed/)) and can be added or edited from the app itself. You can also drop extra profile files into the `SEED_CUSTOM_PATH` folder inside the `plant-data` volume to extend the library without rebuilding anything.

**Look and feel.** Colors, fonts and spacing are defined as design tokens in [`frontend/src/index.css`](frontend/src/index.css) (Tailwind CSS v4), so you can restyle the app without touching component logic.

## Scope

Plant Care App is intentionally small. Multi-user authentication, a native mobile app and cloud sync are out of scope. It is meant to run on your own network as a personal template you can adapt - if you fork it, publish your own images and update the `image:` names in the deploy guide above.

## Documentation

- [`docs/overview.md`](docs/overview.md): feature scope and data model
- [`docs/architecture.md`](docs/architecture.md): system design and key decisions
- [`docs/hardware-integration.md`](docs/hardware-integration.md): optional MQTT device integration and firmware
- [`docs/implementation-plan.md`](docs/implementation-plan.md): build order, testing strategy and acceptance criteria

## Contributing

Contributions are welcome. Keep changes small and scoped - one feature slice per pull request. See [`AGENTS.md`](AGENTS.md) for coding conventions and workflow.

Developed with ASP.NET Core (.NET 10) and EF Core (SQLite, PostgreSQL optional) on the backend, React + Vite + TypeScript with shadcn/ui and Tailwind CSS on the frontend, Coravel for scheduling, and optional MQTT (Mosquitto with ESPHome/Tasmota firmware) for devices.

Every schema change ships with an EF Core migration in the same commit.
