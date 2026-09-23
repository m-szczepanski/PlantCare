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
/// Monthly health-checkup prompts ride the same digest: plants whose last
/// checkup answer is stale get a "checkups due" section, reminded at most
/// once per checkup period per plant (stamped on the plant).
/// </summary>
public sealed class WateringCheckService(
    AppDbContext db,
    IPlantService plants,
    IEnumerable<INotificationChannel> channels,
    QuickActionOptions quickActions,
    IAppLocalizer localizer,
    ILogger<WateringCheckService> logger) : IWateringCheckService
{
    public async Task<WateringCheckResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var all = await plants.ListAsync(cancellationToken);
        var notified = all
            .Where(p => p.NotifyEnabled && (p.SnoozedUntil is null || p.SnoozedUntil <= now))
            .ToList();

        var due = notified
            .Where(p => p.DueStatus is PlantDueStatus.Overdue or PlantDueStatus.DueToday)
            .ToList();

        // Checkup-due plants whose per-plant reminder stamp is missing or stale.
        var checkupCandidateIds = notified.Where(p => p.CheckupDue).Select(p => p.Id).ToList();
        var reminderDates = await db.Plants
            .Where(p => checkupCandidateIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.CheckupReminderSentAt, cancellationToken);
        var checkups = checkupCandidateIds
            .Where(id => reminderDates[id] is null || reminderDates[id] <= now.AddDays(-HealthPolicy.CheckupIntervalDays))
            .Select(id => all.First(p => p.Id == id))
            .ToList();

        if (due.Count == 0 && checkups.Count == 0)
        {
            await LogRunAsync(new WateringCheckResult(0, 0, 0, 0), "nothing-due", cancellationToken);
            return new WateringCheckResult(0, 0, 0, 0);
        }

        var startOfTodayUtc = now.Date;
        if (await db.NotificationDigests.AnyAsync(d => d.SentAt >= startOfTodayUtc, cancellationToken))
        {
            logger.LogInformation("Watering check skipped: digest already sent today.");
            await LogRunAsync(new WateringCheckResult(0, 1, 0, due.Count), "already-sent-today", cancellationToken);
            return new WateringCheckResult(0, 1, 0, due.Count);
        }

        var overdueCount = due.Count(p => p.DueStatus == PlantDueStatus.Overdue);
        var priority = overdueCount > 0 ? 5 : 3;
        var title = due.Count > 0
            ? localizer.Tp("digest.title", due.Count)
            : localizer.Tp("digest.checkups.title", checkups.Count);
        if (overdueCount > 0)
        {
            title += localizer.Tp("digest.overdue", overdueCount);
        }

        var message = string.Join("\n", due.Select(p =>
            $"• {p.NickName}{(p.RoomName is null ? "" : $" ({p.RoomName})")} — {p.DueMessage}"
            + (p.ProfileToxicToPets ? localizer.T("digest.toxicPets") : "")
            + (p.ProfileToxicToChildren ? localizer.T("digest.toxicChildren") : "")));

        if (checkups.Count > 0)
        {
            if (message.Length > 0)
            {
                message += "\n\n";
            }
            message += localizer.Tf("digest.checkups.header", checkups.Count) + "\n"
                + string.Join("\n", checkups.Select(p =>
                    $"• {p.NickName}{(p.RoomName is null ? "" : $" ({p.RoomName})")}"));
        }

        string? clickUrl = string.IsNullOrWhiteSpace(quickActions.PublicBaseUrl)
            ? null
            : quickActions.PublicBaseUrl.TrimEnd('/');
        string? buttonUrl = quickActions.Enabled && due.Count == 1
            ? quickActions.QuickWaterUrl(due[0].Id)
            : null;

        var digest = new NotificationMessage(title, message, priority, clickUrl, buttonUrl, localizer.T("digest.button"));
        var delivered = false;
        foreach (var channel in channels)
        {
            try
            {
                await channel.SendAsync(digest, cancellationToken);
                delivered = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Notification channel {Channel} failed to deliver the watering digest.", channel.Name);
            }
        }

        if (!delivered)
{
            var failure = new WateringCheckResult(0, 0, Math.Max(1, channels.Count()), due.Count);
            await LogRunAsync(failure, "delivery-failed", cancellationToken);
            return failure;
        }

        db.NotificationDigests.Add(new NotificationDigest
        {
            SentAt = now,
            PlantCount = due.Count,
            OverdueCount = overdueCount,
            Priority = priority,
        });

        // The checkup section was delivered — silence the per-plant reminders
        // for a full period (a total delivery failure leaves the stamps alone).
        if (checkups.Count > 0)
        {
            var checkupIds = checkups.Select(c => c.Id).ToList();
            var checkupEntities = await db.Plants
                .Where(p => checkupIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
            foreach (var entity in checkupEntities)
            {
                entity.CheckupReminderSentAt = now;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Watering digest sent for {PlantCount} plant(s) at priority {Priority} ({CheckupCount} checkup reminder(s)).", due.Count, priority, checkups.Count);
        await LogRunAsync(new WateringCheckResult(1, 0, 0, due.Count), "digest-sent", cancellationToken);
        return new WateringCheckResult(1, 0, 0, due.Count);
    }

    private async Task LogRunAsync(WateringCheckResult result, string outcome, CancellationToken cancellationToken)
    {
        db.JobRuns.Add(new JobRunLog
        {
            RanAt = DateTime.UtcNow,
            SentDigests = result.SentDigests,
            SkippedDuplicates = result.SkippedDuplicates,
            Failed = result.Failed,
            Outcome = outcome,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
