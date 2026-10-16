using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

/// <summary>
/// Single source of truth for how the latest health checkup adjusts care
/// schedules and when the next checkup is due. A sick plant drinks less
/// (roots take up less water; overwatering is the bigger risk) and must not
/// be fertilized; an excellent one drinks and feeds a bit sooner. Factors are
/// applied in <see cref="WateringScheduleService"/> on top of the soil-type
/// factor, before the winter doubling.
/// </summary>
public static class HealthPolicy
{
    public const int CheckupIntervalDays = 30;

    private static readonly Dictionary<HealthStatus, double> WateringFactors = new()
    {
        [HealthStatus.Sick] = 1.5,
        [HealthStatus.Bad] = 1.25,
        [HealthStatus.Good] = 1.0,
        [HealthStatus.Excellent] = 0.8,
    };

    private const double ExcellentFertilizingFactor = 0.8;

    private static double WateringFactor(HealthStatus? status)
        => status is { } s ? WateringFactors[s] : 1.0;

    public static int AdjustWateringInterval(HealthStatus? status, int baseIntervalDays)
        => Math.Max(1, (int)Math.Round(baseIntervalDays * WateringFactor(status), MidpointRounding.AwayFromZero));

    public static int AdjustFertilizingInterval(HealthStatus? status, int baseIntervalDays)
        => status == HealthStatus.Excellent
            ? Math.Max(1, (int)Math.Round(baseIntervalDays * ExcellentFertilizingFactor, MidpointRounding.AwayFromZero))
            : baseIntervalDays;

    // Feeding can burn damaged roots.
    public static bool PausesFertilizing(HealthStatus? status) => status is HealthStatus.Sick or HealthStatus.Bad;

    public static DateOnly? NextCheckupDue(Plant plant)
        => plant.LastCheckupAt is { } checkedAt
            ? DateOnly.FromDateTime(checkedAt.Date).AddDays(CheckupIntervalDays)
            : null;

    public static bool IsCheckupDue(Plant plant, DateOnly today)
    {
        if (today.DayNumber - DateOnly.FromDateTime(plant.AcquiredDate.Date).DayNumber < CheckupIntervalDays)
        {
            return false;
        }

        return plant.LastCheckupAt is not { } last
            || today.DayNumber - DateOnly.FromDateTime(last.Date).DayNumber >= CheckupIntervalDays;
    }

    public static bool IsReminderStale(DateTime? lastReminderSentAt, DateTime now)
        => lastReminderSentAt is null || lastReminderSentAt <= now.AddDays(-CheckupIntervalDays);
}
