using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public sealed record WateringCheckResult(int SentDigests, int SkippedDuplicates, int Failed, int PlantsInDigest);

public interface IWateringCheckService
{
    Task<WateringCheckResult> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Daily watering check: collects due/overdue plants (skipping muted ones),
/// publishes ONE digest message to ntfy — escalated to high priority when
/// anything is overdue — and records it in <see cref="NotificationDigest"/>.
/// At most one digest per day; a failed send is logged, never thrown.
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
            .Where(p => p.NotifyEnabled && p.DueStatus is PlantDueStatus.Overdue or PlantDueStatus.DueToday)
            .ToList();

        if (due.Count == 0)
        {
            return new WateringCheckResult(0, 0, 0, 0);
        }

        var startOfTodayUtc = DateTime.UtcNow.Date;
        if (await db.NotificationDigests.AnyAsync(d => d.SentAt >= startOfTodayUtc, cancellationToken))
        {
            logger.LogInformation("Watering check skipped: digest already sent today.");
            return new WateringCheckResult(0, 1, 0, due.Count);
        }

        var overdueCount = due.Count(p => p.DueStatus == PlantDueStatus.Overdue);
        var priority = overdueCount > 0 ? 5 : 3;
        var title = due.Count == 1
            ? "1 plant needs water"
            : $"{due.Count} plants need water";
        if (overdueCount > 0)
        {
            title += $" ({overdueCount} overdue)";
        }

        var message = string.Join("\n", due.Select(p =>
            $"• {p.NickName}{(p.RoomName is null ? "" : $" ({p.RoomName})")} — {p.DueMessage}"));

        try
        {
            await publisher.PublishAsync(title, message, priority, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to send the watering digest; the run ends without blocking the API.");
            return new WateringCheckResult(0, 0, 1, due.Count);
        }

        db.NotificationDigests.Add(new NotificationDigest
        {
            SentAt = DateTime.UtcNow,
            PlantCount = due.Count,
            OverdueCount = overdueCount,
            Priority = priority,
        });
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Watering digest sent for {PlantCount} plant(s) at priority {Priority}.", due.Count, priority);
        return new WateringCheckResult(1, 0, 0, due.Count);
    }
}
