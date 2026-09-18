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
    PlantDueInfo GetDueInfo(Plant plant);

    PlantDueInfo GetDueInfo(Plant plant, DateOnly today);
}

/// <summary>
/// Single source of truth for watering due-date/status logic. Reused by the plant
/// endpoints (step 3), the dashboard (step 5) and the notification job (step 6).
/// </summary>
public sealed class WateringScheduleService : IWateringScheduleService
{
    public PlantDueInfo GetDueInfo(Plant plant) => GetDueInfo(plant, DateOnly.FromDateTime(DateTime.UtcNow.Date));

    public PlantDueInfo GetDueInfo(Plant plant, DateOnly today)
    {
        var intervalDays = plant.CustomWateringIntervalDays ?? plant.PlantProfile?.DefaultWateringIntervalDays;

        if (intervalDays is null or <= 0)
        {
            return new PlantDueInfo(PlantDueStatus.NotScheduled, null, null, null, "No watering schedule");
        }

        var anchor = DateOnly.FromDateTime((plant.LastWateredAt ?? plant.AcquiredDate).Date);
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
