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
    public const int MinWateringCycleForTopUp = 3;

    public const int TopUpCycleDivisor = 2;

    public PlantDueInfo GetDueInfo(CareTask? wateringTask, Plant plant)
        => GetDueInfo(wateringTask, plant, DateOnly.FromDateTime(DateTime.UtcNow.Date));

    public PlantDueInfo GetDueInfo(CareTask? wateringTask, Plant plant, DateOnly today)
    {
        if (wateringTask?.Type == CareTaskType.TopUpWatering)
        {
            var topUp = ComputeTopUp(
                plant.CareTasks.FirstOrDefault(t => t.Type == CareTaskType.Watering),
                wateringTask,
                plant,
                today);
            if (topUp is null)
            {
                return new PlantDueInfo(PlantDueStatus.NotScheduled, null, null, null, localizer.T("due.none"));
            }

            var (topUpStatus, topUpMessage) = StatusAndMessage(topUp.Value.NextDue, today);
            return new PlantDueInfo(
                topUpStatus,
                topUp.Value.Interval,
                topUp.Value.NextDue.DayNumber - today.DayNumber,
                topUp.Value.NextDue,
                topUpMessage);
        }

        var isWatering = (wateringTask?.Type ?? CareTaskType.Watering) == CareTaskType.Watering;

        var intervalDays = wateringTask?.IntervalDays ?? (isWatering
            ? plant.PlantProfile?.DefaultWateringIntervalDays
            : null);
        if (intervalDays is null or <= 0)
        {
            return new PlantDueInfo(PlantDueStatus.NotScheduled, null, null, null, localizer.T("due.none"));
        }

        var (anchor, effectiveInterval) = WateringCycle(wateringTask, plant, today, isWatering);
        var nextDue = anchor.AddDays(effectiveInterval);

        // A soil-wet deferral pushes the due date out to the recheck day (never
        // earlier; watering only) so the plant leaves the due/overdue buckets
        // until the deferral lapses.
        if (isWatering && plant.SoilWetUntil is { } wetUntil)
        {
            var recheck = DateOnly.FromDateTime(wetUntil.Date);
            if (recheck > today && recheck > nextDue)
            {
                nextDue = recheck;
            }
        }

        // A sick/bad checkup answer pauses fertilizing: clamp the due date out to
        // the next checkup day (the soil-wet clamp pattern).
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

        var (nextDue, _) = computed.Value;
        var (status, message) = StatusAndMessage(nextDue, today);
        return new TopUpDueInfo(true, status, nextDue.DayNumber - today.DayNumber, nextDue, message);
    }

    /// <summary>
    /// The mid-cycle top-up: half of the plant's unadjusted watering interval after
    /// the last watering (or the previous top-up), never on/after the next full
    /// watering — the full watering re-anchors and absorbs the pending top-up.
    /// Cycles below <see cref="MinWateringCycleForTopUp"/> days have no room; a
    /// manual <see cref="CareTask.IntervalDays"/> runs its own clock.
    /// </summary>
    private static (DateOnly NextDue, int Interval)? ComputeTopUp(CareTask topUpTask, Plant plant, DateOnly today)
        => ComputeTopUp(plant.CareTasks.FirstOrDefault(t => t.Type == CareTaskType.Watering), topUpTask, plant, today);

    private static (DateOnly NextDue, int Interval)? ComputeTopUp(
        CareTask? wateringTask,
        CareTask topUpTask,
        Plant plant,
        DateOnly today)
    {
        if (wateringTask is null)
        {
            return null;
        }

        if (wateringTask.IntervalDays is null && plant.PlantProfile?.DefaultWateringIntervalDays is null or <= 0)
        {
            return null;
        }

        // The substrate factor is deliberately not applied: the top-up is the
        // permeable-soil answer to the full, unadjusted watering cycle.
        var (wateringAnchor, wateringInterval) = WateringCycle(
            wateringTask,
            plant,
            today,
            isWatering: true,
            applySoil: false);
        if (wateringInterval < MinWateringCycleForTopUp)
        {
            return null;
        }

        var anchor = topUpTask.LastDoneAt is { } done
            ? DateOnly.FromDateTime(done.Date)
            : wateringAnchor;

        if (topUpTask.IntervalDays is { } manual)
        {
            return manual <= 0 ? null : (anchor.AddDays(manual), manual);
        }

        var derived = wateringInterval / TopUpCycleDivisor;
        var nextWateringDue = wateringAnchor.AddDays(wateringInterval);
        if (anchor.AddDays(derived) >= nextWateringDue)
        {
            // No room strictly between waterings: the full watering absorbs the prompt.
            return null;
        }

        return (anchor.AddDays(derived), derived);
    }

    private static (DateOnly Anchor, int EffectiveInterval) WateringCycle(
        CareTask? wateringTask,
        Plant plant,
        DateOnly today,
        bool isWatering,
        bool applySoil = true)
    {
        var intervalDays = wateringTask?.IntervalDays
            ?? (isWatering ? plant.PlantProfile?.DefaultWateringIntervalDays : null);

        var baseInterval = intervalDays ?? 0;
        // Substrate permeability scales only the watering schedule.
        var effectiveInterval = isWatering && applySoil
            ? SoilTypes.AdjustInterval(plant.SoilType, baseInterval)
            : baseInterval;

        // The health answer scales on top of the soil factor.
        if (isWatering)
        {
            effectiveInterval = HealthPolicy.AdjustWateringInterval(plant.HealthStatus, effectiveInterval);
        }
        else if (wateringTask?.Type == CareTaskType.Fertilizing)
        {
            effectiveInterval = HealthPolicy.AdjustFertilizingInterval(plant.HealthStatus, effectiveInterval);
        }

        var reduceInWinter = wateringTask?.ReduceInWinter ?? plant.PlantProfile?.DefaultReduceInWinter ?? false;
        var winter = CareTaskService.IsWinter(today);
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
}
