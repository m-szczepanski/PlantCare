using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public enum DeviceWaterStatus
{
    Success,
    PlantNotFound,
    NoDevice,
    PublishFailed,
}

public sealed record DeviceWaterResult(DeviceWaterStatus Status);

/// <summary>Publishes a "water now" command to a plant's linked device over MQTT.</summary>
public interface IDeviceCommandService
{
    Task<DeviceWaterResult> WaterAsync(int plantId, int? durationMs, CancellationToken cancellationToken = default);
}

public sealed class DeviceCommandService(AppDbContext db, IDeviceBus bus, MqttOptions options, ILogger<DeviceCommandService> logger) : IDeviceCommandService
{
    private const int DefaultDurationMs = 15_000;
    private const int MaxDurationMs = 60_000;

    public async Task<DeviceWaterResult> WaterAsync(int plantId, int? durationMs, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants.FirstOrDefaultAsync(p => p.Id == plantId, cancellationToken);
        if (plant is null)
        {
            return new DeviceWaterResult(DeviceWaterStatus.PlantNotFound);
        }

        if (plant.DeviceId is not { } deviceId)
        {
            return new DeviceWaterResult(DeviceWaterStatus.NoDevice);
        }

        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
        if (device is null)
        {
            return new DeviceWaterResult(DeviceWaterStatus.NoDevice);
        }

        var duration = Math.Clamp(durationMs ?? DefaultDurationMs, 1, MaxDurationMs);
        var topic = string.IsNullOrWhiteSpace(device.CommandTopic)
            ? DeviceTopics.Command(options.TopicPrefix, device.Id)
            : device.CommandTopic;
        var payload = BuildPayload(device.CommandStyle, plantId, duration);

        try
        {
            await bus.PublishAsync(topic, payload, cancellationToken);
            return new DeviceWaterResult(DeviceWaterStatus.Success);
        }
        catch (Exception ex)
        {
            // A publish failure (broker down, device offline) is a failed command,
            // not a crash — the caller maps it to a 502 and the schedule is untouched.
            logger.LogWarning(ex, "Failed to publish water command for plant {PlantId}.", plantId);
            return new DeviceWaterResult(DeviceWaterStatus.PublishFailed);
        }
    }

    private static string BuildPayload(CommandStyle style, int plantId, int durationMs) => style switch
    {
        // Default ESPHome style: the device self-times using durationMs.
        CommandStyle.Pulse => $"{{\"action\":\"water\",\"durationMs\":{durationMs},\"plantId\":{plantId}}}",
        // OnOff/Tasmota: a single "start" command; the device's own auto-off
        // (firmware PulseTime / ESPHome auto-off) is the safety backstop.
        _ => "ON",
    };
}
