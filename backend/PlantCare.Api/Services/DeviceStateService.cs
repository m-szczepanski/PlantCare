using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public sealed record DeviceStateOptions(int WetThresholdPct, int DeferDays);

/// <summary>Parsed state published by a device on <c>plantcare/{id}/state</c>.</summary>
public sealed record DeviceStateMessage(int? MoisturePct, DeviceWaterLevel? WaterLevel, DateTime? Timestamp, bool Watered)
{
    /// <summary>Parses a device state payload; null when the payload is not a JSON object.</summary>
    public static DeviceStateMessage? TryParse(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            int? moisture = null;
            if (root.TryGetProperty("moisturePct", out var m) && m.ValueKind == JsonValueKind.Number && m.TryGetInt32(out var mv))
            {
                moisture = mv;
            }

            DeviceWaterLevel? waterLevel = null;
            if (root.TryGetProperty("waterLevel", out var w) && w.ValueKind == JsonValueKind.String)
            {
                waterLevel = ParseWaterLevel(w.GetString());
            }

            DateTime? timestamp = null;
            if (root.TryGetProperty("ts", out var t) && t.ValueKind == JsonValueKind.String
                && DateTime.TryParse(t.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var tv))
            {
                timestamp = tv;
            }

            var watered = root.TryGetProperty("watered", out var wd) && wd.ValueKind == JsonValueKind.True;

            return new DeviceStateMessage(moisture, waterLevel, timestamp, watered);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DeviceWaterLevel? ParseWaterLevel(string? value) => value?.ToLowerInvariant() switch
    {
        "ok" => DeviceWaterLevel.Ok,
        "low" => DeviceWaterLevel.Low,
        "empty" => DeviceWaterLevel.Empty,
        _ => null,
    };
}

/// <summary>
/// Applies an incoming device state message: mirrors the latest reading on the
/// device and, for a linked plant, defers watering while the soil reads wet
/// (reusing the existing <see cref="Plant.SoilWetUntil"/> deferral) or clears
/// the deferral once it reads dry.
/// </summary>
public interface IDeviceStateIngestor
{
    Task IngestAsync(string topic, string payload, CancellationToken cancellationToken = default);
}

public sealed class DeviceStateIngestor(AppDbContext db, DeviceStateOptions options, ILogger<DeviceStateIngestor> logger, IPlantService plantService, IAppLocalizer localizer) : IDeviceStateIngestor
{
    public async Task IngestAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        if (!DeviceTopics.TryParseStateTopic(topic, out var deviceId))
        {
            logger.LogWarning("Ignoring MQTT message on unexpected topic {Topic}.", topic);
            return;
        }

        var state = DeviceStateMessage.TryParse(payload);
        if (state is null)
        {
            logger.LogWarning("Ignoring malformed device state payload on {Topic}.", topic);
            return;
        }

        if (state.MoisturePct is null && state.WaterLevel is null)
        {
            logger.LogWarning("Ignoring device state with no readable fields on {Topic}.", topic);
            return;
        }

        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
        if (device is null)
        {
            logger.LogWarning("Ignoring state for unknown device {DeviceId}.", deviceId);
            return;
        }

        device.LastMoisturePct = state.MoisturePct;
        device.WaterLevel = state.WaterLevel;
        device.LastSeenAt = state.Timestamp ?? DateTime.UtcNow;

        if (!state.Watered && state.MoisturePct is { } moisture)
        {
            await SyncSoilWetAsync(deviceId, moisture, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (state.Watered)
        {
            await MarkWateredAsync(deviceId, cancellationToken);
        }
    }

    private async Task SyncSoilWetAsync(int deviceId, int moisture, CancellationToken cancellationToken)
    {
        var wet = moisture >= options.WetThresholdPct;
        var until = wet ? DateTime.UtcNow.AddDays(options.DeferDays) : (DateTime?)null;

        var plants = await db.Plants
            .Where(p => p.DeviceId == deviceId)
            .ToListAsync(cancellationToken);

        foreach (var plant in plants)
        {
            plant.SoilWetUntil = until;
        }
    }

    private const int WateredDedupMinutes = 5;

    private async Task MarkWateredAsync(int deviceId, CancellationToken cancellationToken)
    {
        var plantIds = await db.Plants
            .Where(p => p.DeviceId == deviceId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var recentlyWateredBefore = DateTime.UtcNow.AddMinutes(-WateredDedupMinutes);
        foreach (var plantId in plantIds)
        {
            var recent = await db.CareTasks.AnyAsync(t =>
                t.PlantId == plantId && t.Type == CareTaskType.Watering && t.LastDoneAt >= recentlyWateredBefore, cancellationToken);
            if (recent)
            {
                continue;
            }

            await plantService.WaterAsync(plantId, localizer.T("device.wateredNote"), cancellationToken: cancellationToken);
        }
    }
}

/// <summary>
/// Wires the MQTT bus's received messages into <see cref="IDeviceStateIngestor"/>.
/// Runs as a hosted service so each message is handled in its own DI scope
/// (the ingestor needs a scoped <see cref="AppDbContext"/>).
/// </summary>
public sealed class DeviceStateService(IDeviceBus bus, IServiceScopeFactory scopeFactory, ILogger<DeviceStateService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        bus.MessageReceived += OnMessageReceivedAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        bus.MessageReceived -= OnMessageReceivedAsync;
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(string topic, string payload)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var ingestor = scope.ServiceProvider.GetRequiredService<IDeviceStateIngestor>();
            await ingestor.IngestAsync(topic, payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ingest device state from {Topic}.", topic);
        }
    }
}
