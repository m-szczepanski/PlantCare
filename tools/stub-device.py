#!/usr/bin/env python3
"""Stub MQTT device for PlantCare — simulates a self-watering appliance.

Stand-in for real ESP32/ESPHome hardware so the whole feature can be run and
demonstrated with zero hardware. Subscribes to plantcare/+/command and, on a
water command, publishes a "watered" state back to plantcare/<id>/state; it
also emits a periodic moisture reading for a configurable device id.

Usage:
    pip install paho-mqtt
    MQTT_HOST=localhost STUB_DEVICE_ID=1 python3 tools/stub-device.py

Env:
    MQTT_HOST            broker host (default localhost)
    MQTT_PORT            broker port (default 1883)
    MQTT_TOPIC_PREFIX    topic namespace (default plantcare)
    STUB_DEVICE_ID       device id for the periodic reading (default "stub")
    STUB_MOISTURE_PCT    moisture value reported (default 38)
    STUB_INTERVAL_SEC    seconds between periodic readings (default 30)
"""

import json
import os
import time

import paho.mqtt.client as mqtt

HOST = os.environ.get("MQTT_HOST", "localhost")
PORT = int(os.environ.get("MQTT_PORT", "1883"))
PREFIX = os.environ.get("MQTT_TOPIC_PREFIX", "plantcare")
DEVICE_ID = os.environ.get("STUB_DEVICE_ID", "stub")
MOISTURE = int(os.environ.get("STUB_MOISTURE_PCT", "38"))
INTERVAL = int(os.environ.get("STUB_INTERVAL_SEC", "30"))


def state_topic(device_id):
    return f"{PREFIX}/{device_id}/state"


def reading(device_id):
    return json.dumps({
        "moisturePct": MOISTURE,
        "waterLevel": "ok",
        "ts": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
    })


def on_connect(client, _userdata, _flags, _reason_code):
    client.subscribe(f"{PREFIX}/+/command")
    print(f"[stub] connected; subscribed to {PREFIX}/+/command")
    client.publish(state_topic(DEVICE_ID), reading(DEVICE_ID), retain=True)


def on_message(client, _userdata, msg):
    device_id = msg.topic.split("/")[-2]
    print(f"[stub] command for {device_id}: {msg.payload.decode(errors='replace')}")
    state = json.dumps({
        "watered": True,
        "moisturePct": MOISTURE,
        "waterLevel": "ok",
        "ts": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
    })
    client.publish(state_topic(device_id), state, retain=True)
    print(f"[stub] published state to {state_topic(device_id)}")


def main():
    client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
    client.on_connect = on_connect
    client.on_message = on_message

    while True:
        try:
            client.connect(HOST, PORT, 60)
            break
        except OSError as exc:
            print(f"[stub] broker not ready ({exc}); retrying in 2s", flush=True)
            time.sleep(2)

    client.loop_start()
    try:
        while True:
            time.sleep(INTERVAL)
            client.publish(state_topic(DEVICE_ID), reading(DEVICE_ID), retain=True)
    except KeyboardInterrupt:
        client.loop_stop()


if __name__ == "__main__":
    main()
