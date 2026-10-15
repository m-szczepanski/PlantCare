# Device firmware — reference templates

Flash-ready firmware for PlantCare's optional self-watering / monitoring
integration. You do **not** need any of this to build or run the feature —
[`tools/stub-device.py`](../../tools/stub-device.py) stands in for hardware — but
when you buy an ESP32-class board, these are the "default device" the app
expects. The design, topic schema and full config live in
[`../hardware-integration.md`](../hardware-integration.md).

| File | Hardware | Purpose |
|------|----------|---------|
| [`plantcare-esp32.yaml`](plantcare-esp32.yaml) | ESP32 + relay/MOSFET + soil probe | Default device: pump/valve actuation + moisture monitoring (ESPHome) |
| [`plantcare-esp32-sensor.yaml`](plantcare-esp32-sensor.yaml) | ESP32 + soil probe | Read-only moisture node (device type `Sensor`) |
| [`tasmota.md`](tasmota.md) | ESP8266/ESP32 relay board | No-code alternative: a flashed Tasmota relay |

The app also drives **any** MQTT-speaking relay — a generic `OnOff` switch,
Zigbee2MQTT, a Home Assistant switch — so no template is needed for those; set
the device's **command style** and, if the appliance owns its own topic
convention, a **command topic** override in the app.

## The contract

`MQTT_TOPIC_PREFIX` defaults to `plantcare`; `{deviceId}` is the numeric id the
app assigns when you add the device (shown on the Devices page, in the
command/state topic preview).

| Topic | Direction | Payload |
|-------|-----------|---------|
| `plantcare/{deviceId}/command` | app → device | `{"action":"water","durationMs":15000,"plantId":12}` (`Pulse`) or a bare `ON` (`OnOff` / `Tasmota`) |
| `plantcare/{deviceId}/state` | device → app | `{"moisturePct":42,"waterLevel":"ok"}` and, when a watering finishes, `{"watered":true}` |

- `durationMs` is clamped by the app to `WATER_MAX_DURATION_MS` (default 60 000).
- `waterLevel` is `ok` / `low` / `empty`.
- Publish **readings retained** (so a reconnecting app shows the latest value
  immediately) and the one-shot **`watered` event non-retained** (it is an event,
  not a reading, and must not be replayed).
- Unknown JSON fields are ignored on both sides (forward-compatible).
- A per-device **command topic** override is honoured; a per-device **state topic**
  override is stored but **not subscribed yet**, so publish state to the default
  `plantcare/{deviceId}/state`.

## Wiring it up

1. Add the device in the app (**Devices** page) — pick its type and command style —
   and note the id in the topic preview.
2. Put that id and your broker details into the template's `substitutions` /
   `secrets.yaml`.
3. Flash. The device then shows up with live moisture; link it to a plant in the
   plant form.

## Safety

- **Firmware auto-off is the backstop.** Every template cuts the pump after a hard
  cap (`max_water_seconds`, `PulseTime`) even if the app or Wi-Fi dies mid-command.
- Keep the firmware cap **≤** the app's `WATER_MAX_DURATION_MS`.
- The app also enforces a moisture threshold, a per-day cap and soil-wet deferral
  before it ever sends a command.

## Testing without hardware

```bash
# broker + app
ENABLE_DEVICES=true docker compose up -d --build

# a fake device on device id 1
MQTT_HOST=localhost STUB_DEVICE_ID=1 python3 tools/stub-device.py
```

The stub subscribes to `plantcare/+/command` and echoes a `watered` state plus a
periodic moisture reading — enough to exercise the whole loop.
