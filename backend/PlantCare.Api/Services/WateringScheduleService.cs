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
public sealed class WateringScheduleService : IWateringScheduleService
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
            return new PlantDueInfo(PlantDueStatus.NotScheduled, null, null, null, "No watering schedule");
        }

        var anchor = DateOnly.FromDateTime((wateringTask?.LastDoneAt ?? plant.AcquiredDate).Date);
        var nextDue = anchor.AddDays(intervalDays.Value);
        var daysUntilDue = nextDue.DayNumber - today.DayNumber;

        var status = daysUntilDue switch
        {
            < 0 => PlantDueStatus.Overdue,
            0 => PlantDueStatus.DueToday,
            _ => PlantDueStatus.Upcoming,
        };

        var message = status switch
        {
            PlantDueStatus.Overdue => $"{Pluralize(-daysUntilDue, "day")} overdue",
            PlantDueStatus.DueToday => "Due today",
            _ => daysUntilDue == 1 ? "Due tomorrow" : $"{Pluralize(daysUntilDue, "day")} until due",
        };

        return new PlantDueInfo(status, intervalDays, daysUntilDue, nextDue, message);
    }

    private static string Pluralize(int count, string unit) => $"{count} {unit}{(count == 1 ? "" : "s")}";
}
