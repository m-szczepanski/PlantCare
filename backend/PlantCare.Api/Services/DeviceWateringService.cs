using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public sealed record DeviceWateringOptions(
    int MaxDurationMs,
    int MoistureThresholdPct,
    bool AllowWithoutReading,
    int MaxPerDay,
    int FeedbackTimeoutMs);

public sealed record DeviceWateringResult(int Watered, int Skipped, int Failed, int FeedbackFailures);

/// <summary>
/// Automatic watering: for each due plant with a linked device, publish a water
/// command guarded by the moisture threshold, the per-day cap, the duration cap
/// and the soil-wet deferral (via the due-date math). Commands are recorded in
/// <see cref="DeviceWateringLog"/>; unconfirmed ones past the feedback timeout
/// are reported once through the notification channels.
/// </summary>
public interface IDeviceWateringService
{
    Task<DeviceWateringResult> RunAsync(CancellationToken cancellationToken = default);
}

public sealed partial class DeviceWateringService(
    AppDbContext db,
    IPlantService plants,
    IDeviceCommandService commands,
    IEnumerable<INotificationChannel> channels,
    DeviceWateringOptions options,
    IAppLocalizer localizer,
    ILogger<DeviceWateringService> logger) : IDeviceWateringService
{
    public async Task<DeviceWateringResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfToday = now.Date;
        var all = await plants.ListAsync(cancellationToken);

        var candidates = all
            .Where(p => p.DeviceId is not null
                && p.DueStatus is PlantDueStatus.Overdue or PlantDueStatus.DueToday)
            .ToList();

        var watered = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var plant in candidates)
        {
            var sentToday = await db.DeviceWateringLogs
                .CountAsync(l => l.PlantId == plant.Id && l.SentAt >= startOfToday, cancellationToken);
            if (sentToday >= options.MaxPerDay)
            {
                skipped++;
                continue;
            }

            if (plant.SoilMoisturePct is { } moisture)
            {
                if (moisture >= options.MoistureThresholdPct)
                {
                    skipped++; // soil still wet
                    continue;
                }
            }
            else if (!options.AllowWithoutReading)
            {
                skipped++; // no reading yet and not allowed
                continue;
            }

            var result = await commands.WaterAsync(plant.Id, options.MaxDurationMs, cancellationToken);
            switch (result.Status)
            {
                case DeviceWaterStatus.Success:
                    db.DeviceWateringLogs.Add(new DeviceWateringLog
                    {
                        PlantId = plant.Id,
                        DeviceId = plant.DeviceId!.Value,
                        SentAt = now,
                    });
                    watered++;
                    break;
                case DeviceWaterStatus.PublishFailed:
                    failed++;
                    break;
                default:
                    skipped++;
                    break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var feedbackFailures = await ReportFeedbackFailuresAsync(now, cancellationToken);

        LogAutoWaterRun(logger, watered, skipped, failed, feedbackFailures);

        return new DeviceWateringResult(watered, skipped, failed, feedbackFailures);
    }

    private async Task<int> ReportFeedbackFailuresAsync(DateTime now, CancellationToken cancellationToken)
    {
        var threshold = now - TimeSpan.FromMilliseconds(options.FeedbackTimeoutMs);
        var stale = await db.DeviceWateringLogs
            .Where(l => l.ConfirmedAt == null && l.ReportedAt == null && l.SentAt < threshold)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return 0;
        }

        var plantIds = stale.Select(l => l.PlantId).Distinct().ToList();
        var names = await db.Plants
            .Where(p => plantIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.NickName, cancellationToken);

        var message = new NotificationMessage(
            localizer.T("device.feedback.title"),
            localizer.Tf(
                "device.feedback.body",
                string.Join(", ", stale.Select(l => names.GetValueOrDefault(l.PlantId, l.PlantId.ToString())))),
            4);

        foreach (var channel in channels)
        {
            try
            {
                await channel.SendAsync(message, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogChannelFailed(logger, ex, channel.Name);
            }
        }

        foreach (var log in stale)
        {
            log.ReportedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Auto-water run: {Watered} command(s), {Skipped} skipped, {Failed} failed, "
            + "{FeedbackFailures} feedback failure(s).")]
    private static partial void LogAutoWaterRun(
        ILogger logger,
        int watered,
        int skipped,
        int failed,
        int feedbackFailures);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Notification channel {Channel} failed to deliver the device watering feedback.")]
    private static partial void LogChannelFailed(ILogger logger, Exception exception, string channel);
}
