# Tasmota relay — setup

Flashing a Tasmota relay board is the no-code alternative to the ESPHome
template. PlantCare sends a **single** `ON` (command style **Tasmota**) and the
device's own auto-off (`PulseTime`) is the safety backstop — the app never times
the ON→OFF pair itself.

## On the device

1. Flash Tasmota and join Wi-Fi (web installer or serial).
2. Point it at the broker — web UI → **Configuration → MQTT**, or the Console:
   ```
   MqttHost <broker-host>
   MqttPort 1883
   MqttUser <user>        # only if you set MQTT_USERNAME
   MqttPassword <pass>
   Topic plantcare-relay-1
   SetOption19 0          # no Home Assistant discovery
   ```
3. Configure the relay pin for your board (a Tasmota **template** for your module
   does this for you).
4. Set the auto-off, the backstop. `PulseTime` is tenths of a second up to 11.1 s
   and whole seconds above, so 60 s is:
   ```
   PulseTime1 60
   ```
5. *Optional* — report the confirmation when the relay drops. PlantCare keeps the
   schedule consistent on its own when you mark the plant watered, so feedback only
   saves you that click. Publish to the **default** state topic `plantcare/<id>/state`
   (a custom state topic is not subscribed yet):
   ```
   Rule1 ON Power1#State=0 DO Backlog Publish2 plantcare/1/state {"watered":true} END
   Rule1 1
   ```
   Replace `1` with the app's device id, and check the `Publish2` form against your
   Tasmota version (older builds use `Publish` with a different topic prefix).

## In the app

Add the device on the **Devices** page, then set:

| Field | Value |
|-------|-------|
| Type | Valve or Pump |
| Command style | Tasmota |
| Command topic | `cmnd/plantcare-relay-1/POWER` (Tasmota listens here under the default `FullTopic`) |
| State topic | leave blank — uses `plantcare/{id}/state` |

Link the plant. **Water now** publishes `ON` to `cmnd/plantcare-relay-1/POWER`; the
relay closes and auto-off after `PulseTime`. If you set up feedback, the app also
re-anchors the watering schedule when the relay turns off.
