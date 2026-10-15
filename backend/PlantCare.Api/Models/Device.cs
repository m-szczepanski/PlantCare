namespace PlantCare.Api.Models;

public enum DeviceType
{
    Valve,
    Pump,
    Sensor,
}

/// <summary>How a water command is emitted to a device — see <c>docs/hardware-integration.md</c>.</summary>
public enum CommandStyle
{
    Pulse,
    OnOff,
    Tasmota,
}

public enum DeviceWaterLevel
{
    Ok,
    Low,
    Empty,
}

/// <summary>
/// A self-watering / monitoring appliance (ESP32-class) reachable over MQTT.
/// The latest sensor reading is mirrored here so display and safety guards
/// never touch a history table.
/// </summary>
public class Device
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DeviceType Type { get; set; }

    public CommandStyle CommandStyle { get; set; } = CommandStyle.Pulse;

    /// <summary>MQTT command-topic override; empty → <c>plantcare/{id}/command</c>.</summary>
    public string? CommandTopic { get; set; }

    /// <summary>MQTT state-topic override; empty → <c>plantcare/{id}/state</c>.</summary>
    public string? StateTopic { get; set; }

    /// <summary>Latest soil-moisture reading in percent (0–100).</summary>
    public int? LastMoisturePct { get; set; }

    public DeviceWaterLevel? WaterLevel { get; set; }

    /// <summary>When the device last reported a reading.</summary>
    public DateTime? LastSeenAt { get; set; }

    public List<Plant> Plants { get; set; } = [];
}
