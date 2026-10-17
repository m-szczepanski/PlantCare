# Hardware Integration — Design & Implementation Plan

> Status: **complete** (Epic 11, all chunks + frontend UI). The full feature is
> implemented and gated by `ENABLE_DEVICES` (default off): `Device` registry +
> plant link, MQTTnet bus + Mosquitto + `tools/stub-device.py`, state ingestion +
> wet-reading soil-wet deferral, manual `POST /api/plants/{id}/device-water` +
> `watered:true` feedback, the scheduled auto-water job with safety guards, and
> the frontend Devices page + plant-form device picker + "Water now"/moisture UI.
> The chunk breakdown and acceptance criteria live in `improvement-plan.md` §
> Epic 11; this document is the design decision record and the source of truth
> for the transport, topic schema, data model and safety rules.

## Goal

Let a home user connect ESP32-class self-watering / monitoring hardware to
PlantCare so that:

- the app's existing watering schedule drives **automatic watering**
  (actuation — the app tells a pump/valve to run), and
- sensor readings (soil moisture, water level) flow **into** the app for
  display and as a safety guard (monitoring).

Single-user, LAN-only, no cloud. Opt-in and **off by default**.

## Non-goals

- No cloud relay or vendor lock-in (ESPHome/Tasmota + MQTT are all local).
- No Home Assistant as a hard dependency — optional, never required.
- No closed-loop PID irrigation; a schedule + a moisture-threshold guard is
  enough for home use.
- Not replacing manual "mark as watered" — auto-watering is per-plant opt-in.
- No multi-zone / one-device-many-plants routing in the MVP (the topic schema
  leaves room for it later, but v1 of this feature is one device per plant).

## Architecture

```
                 MQTT (Mosquitto)
ESP32 device ◄───────────────► api (MQTTnet client)
 (ESPHome/Tasmota)  publish/subscribe    │
                            ┌───────────┤
                            │           │
                        SQLite     Coravel jobs
                                     (digest + device watering)
```

The app **remains the brain**. All schedule math (`WateringScheduleService`)
stays unchanged; hardware is one more consumer of "is this plant due?" and one
more producer of "this plant was watered."

## Decisions

1. **MQTT (Mosquitto) is the transport.** ESPHome/Tasmota devices speak MQTT
   natively, and a single self-hosted Mosquitto container mirrors the existing
   `ntfy` pattern (LAN sidecar, no extra platform). This resolves the
   long-deferred `feature/room-sensors` "which source?" question in favour of
   MQTT, with Home Assistant as an optional peer on the same broker.
2. **Firmware is ESPHome or Tasmota, not bespoke.** No custom firmware to
   write or maintain; the ESP32 is configured declaratively and wired to the
   topics below.
3. **Actuation is a sibling job, not a new channel.** `INotificationChannel`
   pushes notifications; a device should act, not notify. A separate
   `DeviceWateringJob` reuses `IPlantService`/`WateringScheduleService` (the
   single source of truth) and never touches the digest/notification path.
4. **Feedback maps to the existing mark-done logic.** When a device reports
   `watered: true`, the backend re-anchors the watering `CareTask` through the
   same code as `POST /api/plants/{id}/water`, so schedule, dashboard, ICS and
   insights all stay consistent with zero duplication.
5. **Env-gated and off by default.** `ENABLE_DEVICES=false` keeps v1 behaviour
   identical for anyone not using hardware.

## Interoperability

MQTT is the universal transport, so the same backend drives the common
ESP32-class firmwares and any MQTT-speaking appliance (Zigbee2MQTT, Home
Assistant switches, etc.) without vendor lock-in. The default target is
**ESPHome on an ESP32**; supporting another appliance is a per-device
*configuration* choice, never bespoke code.

The flexibility lever is a **command style** stored on each `Device` (plus
optional topic overrides):

| Style | How a water command is emitted | Typical target |
|-------|--------------------------------|----------------|
| `Pulse` (default) | one message `{"action":"water","durationMs":N}`; device self-times | ESPHome script / smart pump |
| `OnOff` | publish a single `ON`; the device's own auto-off is the backstop | generic relay, Zigbee2MQTT, HA switch |
| `Tasmota` | publish a single `ON` to `cmnd/<topic>/POWER`; Tasmota `PulseTime` auto-offs | Tasmota relay |

`CommandTopic` / `StateTopic` overrides map an appliance that already owns its
MQTT convention (e.g. `zigbee2mqtt/<name>/set`); left empty they default to
`plantcare/{id}/command|state`. Payload shapes are fixed per style (constants,
not per-device free-form) so the backend stays "to the point".

Reference firmware templates (ESPHome default, plus a Tasmota equivalent) ship
under [`docs/firmware/`](firmware/README.md) — the flash-ready "default device",
written before any hardware is owned. The ESPHome template drives a relay and
parses `durationMs` from the command, with a firmware-level auto-off as the
backstop; the Tasmota equivalent is a single `ON` on `cmnd/<topic>/POWER` with
the board's `PulseTime` doing the auto-off — no custom firmware at all. Both
publish readings retained and the `watered` event non-retained.

## Topic schema

`MQTT_TOPIC_PREFIX` defaults to `plantcare`; `{deviceId}` is the device's id,
which doubles as its MQTT identifier.

| Topic | Direction | Payload (JSON) |
|-------|-----------|----------------|
| `plantcare/{deviceId}/command` | app → device | `{"action":"water","durationMs":15000,"plantId":12}` |
| `plantcare/{deviceId}/state` | device → app | reading: `{"moisturePct":42,"waterLevel":"ok","ts":"2026-10-15T08:00:00Z"}` · event: `{"watered":true}` |

- Publish **readings retained** (so a reconnecting app immediately sees the
  latest value) and the one-shot **`watered` event non-retained** (it is an event,
  not a reading, and must not be replayed).
- `waterLevel` is `ok` / `low` / `empty`; `ts` is optional (defaults to receipt
  time).
- The `command` payload above is the `Pulse` style; `OnOff`/`Tasmota` send a
  single plain `ON` instead (the device self-times) — see Interoperability.
- Unknown JSON fields are ignored on both sides (forward-compatible).

## Data model

```
Device
- Id (int — also the MQTT identifier)
- Name
- Type (enum: Valve / Pump / Sensor, stored as text)   // Sensor = read-only, no actuation
- CommandStyle (enum: Pulse / OnOff / Tasmota, stored as text, default Pulse)
- CommandTopic (string?, nullable)   // override; empty → plantcare/{id}/command
- StateTopic (string?, nullable)     // override; stored, but not subscribed yet
- LastMoisturePct (int?, nullable)
- WaterLevel (string, nullable: ok/low/empty)
- LastSeenAt (instant, nullable)

DeviceWateringLog   // one row per auto-water command, for confirmation + dedup
- Id, PlantId (FK), DeviceId (FK), SentAt, ConfirmedAt (nullable), ReportedAt (nullable)

Plant.DeviceId (int?, nullable FK → Device, SetNull on delete)
```

The latest reading is **mirrored on the `Device`** (same pattern as
`HealthStatus`/`LastCheckupAt` on `Plant`) so display and the safety guard never
touch a history table. A per-reading history log (`DeviceReading`) is a later
optional add, not part of the MVP.

Migrations: `AddDevices` (new `Devices` table + `Plant.DeviceId` column) and
`AddDeviceWateringLog`, provider-portable, committed with their chunks.

## API surface

```
GET    /api/devices                list devices (+ latest reading, plant count)
POST   /api/devices                create {name, type, commandStyle, commandTopic?, stateTopic?}
GET    /api/devices/{id}           single device
PUT    /api/devices/{id}           update
DELETE /api/devices/{id}           delete (plants keep DeviceId → set null)
POST   /api/plants/{id}/device-water   manual "water now" — publish a command to the plant's device
```

Plant create/update accepts `deviceId`; plant DTOs carry `deviceId`,
`soilMoisturePct` and `soilMoistureAgeMinutes` (age of the last reading, null
when none). The device endpoints and the plant DTO fields are all behind
`ENABLE_DEVICES`.

## Safety rules (the actual work in slice 3)

- **Moisture guard:** auto-water only when the latest reading is below
  `MOISTURE_AUTO_WATER_THRESHOLD_PCT` (default 40), or when the device has no
  reading yet *and* `ALLOW_AUTO_WATER_WITHOUT_READING=true` (default false —
  skip and log, safer for a first-time device).
- **Duration cap:** every command's `durationMs` is clamped to
  `WATER_MAX_DURATION_MS` (default 60 000).
- **Per-day cap:** at most `AUTO_WATER_MAX_PER_DAY` (default 1) command per
  device per day.
- **Respect soil-wet deferrals:** `SoilWetUntil` ("soil still wet") is honoured
  because the due-plant collection reuses `WateringScheduleService`, and a wet
  sensor reading maps onto the same deferral. Vacation snooze does **not** block
  auto-watering — `SnoozedUntil` is a notification-only mute and watering while
  away is the point; the moisture and per-day guards are the real safety net.
- **Feedback timeout:** if a device does not report `watered: true` within
  `WATER_FEEDBACK_TIMEOUT_MS`, the run logs the failure, surfaces it in the
  digest, and does **not** auto-retry the same day.
- **No blind retries:** a watering is only logged/re-anchored on device
  confirmation, never on command send.

## Configuration (env-driven)

| Variable | Purpose | Default |
|----------|---------|---------|
| `ENABLE_DEVICES` | Feature flag for all hardware surfaces | `false` |
| `MQTT_HOST` / `MQTT_PORT` | Broker address (container name in compose) | `mqtt` / `1883` |
| `MQTT_USERNAME` / `MQTT_PASSWORD` | Optional broker auth (LAN default: none) | empty |
| `MQTT_TOPIC_PREFIX` | Topic namespace | `plantcare` |
| `MOISTURE_WET_THRESHOLD_PCT` | Reading at/above this defers the plant (soil-wet) | `60` |
| `SOIL_WET_DEFER_DAYS` | Days a wet reading pushes the due date out | `1` |
| `DEVICE_WATERING_CRON` | Cron for the auto-water job | `0 8 * * *` |
| `WATER_MAX_DURATION_MS` | Hard cap on command duration | `60000` |
| `MOISTURE_AUTO_WATER_THRESHOLD_PCT` | Wet threshold for auto-water | `40` |
| `ALLOW_AUTO_WATER_WITHOUT_READING` | Auto-water with no reading yet | `false` |
| `AUTO_WATER_MAX_PER_DAY` | Max commands per device per day | `1` |
| `WATER_FEEDBACK_TIMEOUT_MS` | Wait for device confirmation | `30000` |

## Docker Compose

```yaml
mqtt:
  image: eclipse-mosquitto:2
  restart: unless-stopped
  ports:
    - "1883:1883"        # LAN only; not exposed to the internet
  volumes:
    - mqtt-data:/mosquitto/data
    - ./mosquitto/mosquitto.conf:/mosquitto/config/mosquitto.conf:ro
```

Auth is optional for a LAN-only install; the ESP32 and the API connect on the
internal network by default.

## Testing strategy

**Hardware is not required to build or verify this feature.** The backend
(transport, scheduling, guards, feedback loop) is exercised entirely against a
recording `IDeviceBus` fake and a real Mosquitto broker; physical relay/pump
wiring only matters for the final smoke test, which the user can run later when
they own a device.

- **Simulated device (first-class deliverable).** `tools/stub-device` (Python +
  `paho-mqtt`) subscribes to the command topic and publishes `watered`/
  `moisturePct` state exactly as real hardware would. It lets the whole feature
  run and be demonstrated end to end with zero hardware.
- **Unit tests** (recording `IDeviceBus`, mirroring the `INtfyPublisher` fake):
  guard logic (threshold, duration cap, per-day cap, feedback timeout), command
  style selection, and the feedback → mark-done mapping.
- **Integration tests** (WebApplicationFactory): command emission (correct topic
  + payload per style) and reading ingestion (DTO surface updates).
- **Manual:** `docker compose up` + `stub-device` — verify a reading lands on the
  plant card, "Water now" publishes, and a due plant auto-waters exactly once
  with a wet sensor suppressing it.

## Rollout (slices → chunks)

1. **Monitoring first** (`device-model` + `mqtt-bus` + `soil-moisture-monitoring`)
   — read-only, zero actuation risk, proves the MQTT path end to end.
2. **Manual water** (`manual-water`) — human in the loop, proves the command
   path and the feedback → mark-done loop.
3. **Scheduled auto-water** (`scheduled-auto-water`) — the safety rules above,
   gated behind `ENABLE_DEVICES` and the cron.

## Diagnostics & test bench (Epic 12, planned)

Setup-time tooling so a user who just wired a pump/sensor can verify the MQTT
path end to end without waiting for the cron, forcing plant state, or reading
server logs. Gated by `ENABLE_DEVICES`; **no schema change** — recent traffic is
a bounded in-memory ring buffer, not a table. Chunks + acceptance criteria live
in `improvement-plan.md` § Epic 12.

```
GET  /api/devices/{id}/diagnostics    broker/device state + resolved topics + recent messages
GET  /api/status                      (extended) broker connectivity
POST /api/devices/{id}/test-command   publish a command, wait for the watered ack
POST /api/devices/{id}/test-reading   publish a synthetic reading to the state topic
POST /api/devices/{id}/run-auto-water run the scheduled auto-water pass now
```

Decisions:

- **Round-trip through the broker.** Test readings are published to the real
  state topic so the whole transport *and* ingestion path is exercised; when the
  broker is down the panel reports an explicit "not connected" state rather than
  a silent no-op.
- **Guard isolation.** Test commands are never written to `DeviceWateringLog` and
  never count against `AUTO_WATER_MAX_PER_DAY` — probing must not consume the
  auto-water budget. Only a device's own `watered:true` echo re-anchors the
  schedule (unchanged behaviour).
- **Real-plant side effect.** A test command sent to a device linked to a plant
  will actually water it and re-anchor its schedule; the UI confirms before
  sending and recommends an unlinked test device.
- **Ack correlation.** `test-command` listens on the bus's `MessageReceived` for
  the matching `watered:true` and reports `{published, acked, elapsedMs}`; a
  timeout is a valid, non-error outcome. A publish while disconnected returns a
  distinct result instead of the generic `PublishFailed`.
- **Recent-traffic budget.** The ring buffer is bounded (e.g. the last 50
  messages) and reset on restart — a diagnostic, not an audit log.
- **Known gap surfaced.** Custom per-device `StateTopic` overrides are stored but
  not yet subscribed (only `{prefix}/+/state` is); the panel shows the resolved
  state topic so a mismatch reads as configuration, not broken hardware. A fix
  can ride this epic.

When implemented, update:

- `backend/docs/README.md` / `frontend/docs/README.md` — diagnostics endpoints +
  test panel.
- Root `README.md` — a "test your device" step pointing at the panel.

## When implemented, update

- `docs/architecture.md` — add the `mqtt` service and the MQTT data flow. ✅
- `docs/overview.md` — list devices in the data model, API surface and compose. ✅
- `backend/docs/README.md` — device endpoints + `MQTT_*`/`WATER_*` config table. ✅
- `frontend/docs/README.md` — devices page, "Water now" button, moisture display. ✅
- `.env.example` — the new variables above. ✅
- Root `README.md` — hardware section (wiring guide, topic table). ✅
- `docs/firmware/` — ship the ESPHome (default) + Tasmota reference templates and
  the `tools/stub-device` simulator, so the feature is runnable and flashable
  without owned hardware. ✅
