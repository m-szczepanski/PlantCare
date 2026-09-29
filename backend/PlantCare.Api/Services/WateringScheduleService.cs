using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public sealed record PlantDueInfo(
    PlantDueStatus Status,
    int? IntervalDays,
    int? DaysUntilDue,
    DateOnly? NextDueDate,
    string Message);

public sealed record TopUpDueInfo(
    bool Enabled,
    PlantDueStatus Status,
    int? DaysUntilDue,
    DateOnly? NextDueDate,
    string? Message);

public interface IWateringScheduleService
{
    PlantDueInfo GetDueInfo(CareTask? wateringTask, Plant plant);

    PlantDueInfo GetDueInfo(CareTask? wateringTask, Plant plant, DateOnly today);

    TopUpDueInfo GetTopUpDueInfo(CareTask? wateringTask, CareTask? topUpTask, Plant plant);

    TopUpDueInfo GetTopUpDueInfo(CareTask? wateringTask, CareTask? topUpTask, Plant plant, DateOnly today);
}

/// <summary>Single source of truth for watering (and watering-derived top-up) due-date/status logic.</summary>
public sealed class WateringScheduleService(IAppLocalizer localizer) : IWateringScheduleService
{
    /// <summary>Cycles shorter than this are too tight to slip a top-up into.</summary>
    public const int MinWateringCycleForTopUp = 3;

    /// <summary>Highly permeable soils get a small "top up" drink at this fraction of the watering cycle.</summary>
    public const int TopUpCycleDivisor = 2;

    public PlantDueInfo GetDueInfo(CareTask? wateringTask, Plant plant)
        => GetDueInfo(wateringTask, plant, DateOnly.FromDateTime(DateTime.UtcNow.Date));

    public PlantDueInfo GetDueInfo(CareTask? wateringTask, Plant plant, DateOnly today)
    {
        if (wateringTask?.Type == CareTaskType.TopUpWatering)
        {
            var topUp = ComputeTopUp(plant.CareTasks.FirstOrDefault(t => t.Type == CareTaskType.Watering), wateringTask, plant, today);
            if (topUp is null)
            {
                return new PlantDueInfo(PlantDueStatus.NotScheduled, null, null, null, localizer.T("due.none"));
            }

            var (topUpStatus, topUpMessage) = StatusAndMessage(topUp.Value.NextDue, today);
            return new PlantDueInfo(topUpStatus, topUp.Value.Interval, topUp.Value.NextDue.DayNumber - today.DayNumber, topUp.Value.NextDue, topUpMessage);
        }

        var isWatering = (wateringTask?.Type ?? CareTaskType.Watering) == CareTaskType.Watering;

        var intervalDays = wateringTask?.IntervalDays ?? (isWatering ? plant.PlantProfile?.DefaultWateringIntervalDays : null);
        if (intervalDays is null or <= 0)
        {
            return new PlantDueInfo(PlantDueStatus.NotScheduled, null, null, null, localizer.T("due.none"));
        }

        var (anchor, effectiveInterval) = WateringCycle(wateringTask, plant, today, isWatering);
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

        var (status, message) = StatusAndMessage(nextDue, today);

        return new PlantDueInfo(status, effectiveInterval, nextDue.DayNumber - today.DayNumber, nextDue, message);
    }

    public TopUpDueInfo GetTopUpDueInfo(CareTask? wateringTask, CareTask? topUpTask, Plant plant)
        => GetTopUpDueInfo(wateringTask, topUpTask, plant, DateOnly.FromDateTime(DateTime.UtcNow.Date));

    public TopUpDueInfo GetTopUpDueInfo(CareTask? wateringTask, CareTask? topUpTask, Plant plant, DateOnly today)
    {
        if (topUpTask is null)
        {
            return new TopUpDueInfo(false, PlantDueStatus.NotScheduled, null, null, null);
        }

        var computed = ComputeTopUp(topUpTask, plant, today);
        if (computed is null)
        {
            return new TopUpDueInfo(true, PlantDueStatus.NotScheduled, null, null, localizer.T("due.none"));
        }

        var (nextDue, interval) = computed.Value;
        var (status, message) = StatusAndMessage(nextDue, today);
        return new TopUpDueInfo(true, status, nextDue.DayNumber - today.DayNumber, nextDue, message);
    }

    /// <summary>
    /// The mid-cycle top-up derived from the watering schedule: due at half of the
    /// plant's (unadjusted) watering interval after the last watering — or after
    /// the previous top-up — never on/after the next full watering, which both
    /// re-anchors the cycle and absorbs any pending top-up. Cycles below
    /// <see cref="MinWateringCycleForTopUp"/> days have no room between waterings
    /// and report "not scheduled". A manual <see cref="CareTask.IntervalDays"/> on
    /// the top-up task replaces the derived half-cycle with its own clock.
    /// </summary>
    private (DateOnly NextDue, int Interval)? ComputeTopUp(CareTask topUpTask, Plant plant, DateOnly today)
        => ComputeTopUp(plant.CareTasks.FirstOrDefault(t => t.Type == CareTaskType.Watering), topUpTask, plant, today);

    private (DateOnly NextDue, int Interval)? ComputeTopUp(CareTask? wateringTask, CareTask topUpTask, Plant plant, DateOnly today)
    {
        if (wateringTask is null)
        {
            return null;
        }

        if (wateringTask.IntervalDays is null && plant.PlantProfile?.DefaultWateringIntervalDays is null or <= 0)
        {
            return null;
        }

        // The substrate factor is deliberately NOT applied here: the top-up is the
        // permeable-soil answer to the *full, unadjusted* watering cycle (the mix's
        // fast drainage is exactly why the drink in between exists).
        var (wateringAnchor, wateringInterval) = WateringCycle(wateringTask, plant, today, isWatering: true, applySoil: false);
        if (wateringInterval < MinWateringCycleForTopUp)
        {
            return null;
        }

        var anchor = topUpTask.LastDoneAt is { } done
            ? DateOnly.FromDateTime(done.Date)
            : wateringAnchor;

        if (topUpTask.IntervalDays is { } manual)
        {
            // A manual cadence runs on its own clock; it is kept as-is.
            return manual <= 0 ? null : (anchor.AddDays(manual), manual);
        }

        var derived = wateringInterval / TopUpCycleDivisor;
        var nextWateringDue = wateringAnchor.AddDays(wateringInterval);
        if (anchor.AddDays(derived) >= nextWateringDue)
        {
            // No room left strictly between waterings (slow, retentive mixes, or
            // the cycle's top-up already taken). The full watering absorbs it —
            // report "not scheduled".
            return null;
        }

        return (anchor.AddDays(derived), derived);
    }

    private (DateOnly Anchor, int EffectiveInterval) WateringCycle(CareTask? wateringTask, Plant plant, DateOnly today, bool isWatering, bool applySoil = true)
    {
        var intervalDays = wateringTask?.IntervalDays
            ?? (isWatering ? plant.PlantProfile?.DefaultWateringIntervalDays : null);

        var baseInterval = intervalDays ?? 0;
        // Substrate permeability only scales the watering schedule (not fertilizing/repotting).
        var effectiveInterval = isWatering && applySoil ? SoilTypes.AdjustInterval(plant.SoilType, baseInterval) : baseInterval;

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
        return (anchor, effectiveInterval);
    }

    private (PlantDueStatus Status, string Message) StatusAndMessage(DateOnly nextDue, DateOnly today)
    {
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

        return (status, message);
    }

    /// <summary>Winter months for the (northern-hemisphere) seasonal reduction. Northern users only for v1.</summary>
    private static bool isWinter(DateOnly today) => today.Month is 12 or 1 or 2;
}
