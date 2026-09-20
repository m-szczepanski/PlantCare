using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public sealed record PlantDueInfo(
    PlantDueStatus Status,
    int? IntervalDays,
    int? DaysUntilDue,
    DateOnly? NextDueDate,
    string Message);

public interface IWateringScheduleService
{
    PlantDueInfo GetDueInfo(CareTask? wateringTask, Plant plant);

    PlantDueInfo GetDueInfo(CareTask? wateringTask, Plant plant, DateOnly today);
}

/// <summary>Single source of truth for watering due-date/status logic.</summary>
public sealed class WateringScheduleService(IAppLocalizer localizer) : IWateringScheduleService
{
    public PlantDueInfo GetDueInfo(CareTask? wateringTask, Plant plant)
        => GetDueInfo(wateringTask, plant, DateOnly.FromDateTime(DateTime.UtcNow.Date));

    public PlantDueInfo GetDueInfo(CareTask? wateringTask, Plant plant, DateOnly today)
    {
        var intervalDays = wateringTask?.IntervalDays
            ?? (((wateringTask?.Type ?? CareTaskType.Watering) == CareTaskType.Watering)
                ? plant.PlantProfile?.DefaultWateringIntervalDays
                : null);

        if (intervalDays is null or <= 0)
        {
            return new PlantDueInfo(PlantDueStatus.NotScheduled, null, null, null, localizer.T("due.none"));
        }

        var reduceInWinter = wateringTask?.ReduceInWinter ?? plant.PlantProfile?.DefaultReduceInWinter ?? false;
        var winter = isWinter(today);
        var effectiveInterval = reduceInWinter && winter ? intervalDays.Value * 2 : intervalDays.Value;

        var anchor = DateOnly.FromDateTime((wateringTask?.LastDoneAt ?? plant.AcquiredDate).Date);
        var nextDue = anchor.AddDays(effectiveInterval);
        var daysUntilDue = nextDue.DayNumber - today.DayNumber;

        var status = daysUntilDue switch
        {
            < 0 => PlantDueStatus.Overdue,
            0 => PlantDueStatus.DueToday,
            _ => PlantDueStatus.Upcoming,
        };

        var message = status switch
        {
            PlantDueStatus.Overdue => localizer.Tp("due.overdue", -daysUntilDue),
            PlantDueStatus.DueToday => localizer.T("due.today"),
            _ => daysUntilDue == 1 ? localizer.T("due.tomorrow") : localizer.Tp("due.until", daysUntilDue),
        };

        return new PlantDueInfo(status, effectiveInterval, daysUntilDue, nextDue, message);
    }

    /// <summary>Winter months for the (northern-hemisphere) seasonal reduction. Northern users only for v1.</summary>
    private static bool isWinter(DateOnly today) => today.Month is 12 or 1 or 2;
}
