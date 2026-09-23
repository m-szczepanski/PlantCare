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
        var isWatering = (wateringTask?.Type ?? CareTaskType.Watering) == CareTaskType.Watering;

        var intervalDays = wateringTask?.IntervalDays
            ?? (isWatering ? plant.PlantProfile?.DefaultWateringIntervalDays : null);

        if (intervalDays is null or <= 0)
        {
            return new PlantDueInfo(PlantDueStatus.NotScheduled, null, null, null, localizer.T("due.none"));
        }

        var baseInterval = intervalDays.Value;
        // Substrate permeability only scales the watering schedule (not fertilizing/repotting).
        var effectiveInterval = isWatering ? SoilTypes.AdjustInterval(plant.SoilType, baseInterval) : baseInterval;

        // The latest health checkup answer scales intervals on top of the soil
        // factor (watering every status; fertilizing only the thriving plants).
        if (isWatering)
        {
            effectiveInterval = HealthPolicy.AdjustWateringInterval(plant.HealthStatus, effectiveInterval);
        }
        else if (wateringTask?.Type == CareTaskType.Fertilizing)
        {
            effectiveInterval = HealthPolicy.AdjustFertilizingInterval(plant.HealthStatus, effectiveInterval);
        }

        var reduceInWinter = wateringTask?.ReduceInWinter ?? plant.PlantProfile?.DefaultReduceInWinter ?? false;
        var winter = isWinter(today);
        effectiveInterval = reduceInWinter && winter ? effectiveInterval * 2 : effectiveInterval;

        var anchor = DateOnly.FromDateTime((wateringTask?.LastDoneAt ?? plant.AcquiredDate).Date);
        var nextDue = anchor.AddDays(effectiveInterval);

        // A "soil still wet" deferral pushes the watering due date out to the recheck
        // day (never earlier; watering only). The plant leaves the overdue/due buckets
        // and shows as upcoming, and the daily digest skips it in turn. On the recheck
        // day the deferral has lapsed and the plant becomes due again.
        if (isWatering && plant.SoilWetUntil is { } wetUntil)
        {
            var recheck = DateOnly.FromDateTime(wetUntil.Date);
            if (recheck > today && recheck > nextDue)
            {
                nextDue = recheck;
            }
        }

        // A sick/bad checkup answer pauses fertilizing until the next checkup day
        // (the same clamp pattern as the soil-wet deferral): the plant drops out of
        // the due buckets and resurfaces when the answer goes stale and is re-asked.
        if (!isWatering && wateringTask?.Type == CareTaskType.Fertilizing
            && HealthPolicy.PausesFertilizing(plant.HealthStatus)
            && HealthPolicy.NextCheckupDue(plant) is { } resume
            && resume > today && resume > nextDue)
        {
            nextDue = resume;
        }

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
