using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public sealed record WateringCheckResult(int Sent, int SkippedDuplicates, int Failed);

public interface IWateringCheckService
{
    Task<WateringCheckResult> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Daily watering check: finds due/overdue plants via the shared due-status
/// service, publishes ntfy reminders and records them in <see cref="NotificationLog"/>.
/// At most one notification per plant per day; a failed send is logged and never
/// aborts the run or blocks the remaining plants.
/// </summary>
public sealed class WateringCheckService(
    AppDbContext db,
    IPlantService plants,
    INtfyPublisher publisher,
    ILogger<WateringCheckService> logger) : IWateringCheckService
{
    public async Task<WateringCheckResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var due = (await plants.ListAsync(cancellationToken))
            .Where(p => p.DueStatus is PlantDueStatus.Overdue or PlantDueStatus.DueToday)
            .ToList();

        var startOfTodayUtc = DateTime.UtcNow.Date;
        var notifiedToday = await db.NotificationLogs
            .Where(n => n.Type == NotificationType.WateringDue && n.SentAt >= startOfTodayUtc)
            .Select(n => n.PlantId)
            .ToListAsync(cancellationToken);

        var sent = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var plant in due)
        {
            if (notifiedToday.Contains(plant.Id))
            {
                skipped++;
                continue;
            }

            try
            {
                await publisher.PublishAsync(
                    "Watering due",
                    $"{plant.NickName} ({plant.Location}) — {plant.DueMessage}",
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to send ntfy notification for plant {PlantId}; continuing with the rest.", plant.Id);
                failed++;
                continue;
            }

            db.NotificationLogs.Add(new NotificationLog
            {
                PlantId = plant.Id,
                Type = NotificationType.WateringDue,
                SentAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
            sent++;
        }

        logger.LogInformation("Watering check finished: {Sent} sent, {Skipped} skipped (dedup), {Failed} failed.", sent, skipped, failed);
        return new WateringCheckResult(sent, skipped, failed);
    }
}
