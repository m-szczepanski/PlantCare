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
    /// <summary>How often a plant should get a health checkup.</summary>
    public const int CheckupIntervalDays = 30;

    private static readonly Dictionary<HealthStatus, double> WateringFactors = new()
    {
        [HealthStatus.Sick] = 1.5,
        [HealthStatus.Bad] = 1.25,
        [HealthStatus.Good] = 1.0,
        [HealthStatus.Excellent] = 0.8,
    };

    public const double ExcellentFertilizingFactor = 0.8;

    public static double WateringFactor(HealthStatus? status)
        => status is { } s ? WateringFactors[s] : 1.0;

    /// <summary>Applies the health factor to a base watering interval, never below one day.</summary>
    public static int AdjustWateringInterval(HealthStatus? status, int baseIntervalDays)
        => Math.Max(1, (int)Math.Round(baseIntervalDays * WateringFactor(status), MidpointRounding.AwayFromZero));

    /// <summary>A thriving plant is in active growth and eats a bit sooner; others keep the interval.</summary>
    public static int AdjustFertilizingInterval(HealthStatus? status, int baseIntervalDays)
        => status == HealthStatus.Excellent
            ? Math.Max(1, (int)Math.Round(baseIntervalDays * ExcellentFertilizingFactor, MidpointRounding.AwayFromZero))
            : baseIntervalDays;

    /// <summary>Never fertilize a plant that is doing badly — feeding can burn damaged roots.</summary>
    public static bool PausesFertilizing(HealthStatus? status) => status is HealthStatus.Sick or HealthStatus.Bad;

    /// <summary>The day the current checkup answer goes stale and the next prompt starts.</summary>
    public static DateOnly? NextCheckupDue(Plant plant)
        => plant.LastCheckupAt is { } checkedAt
            ? DateOnly.FromDateTime(checkedAt.Date).AddDays(CheckupIntervalDays)
            : null;

    /// <summary>Due when the plant has outlived the grace period and has no fresh checkup answer.</summary>
    public static bool IsCheckupDue(Plant plant, DateOnly today)
    {
        if (today.DayNumber - DateOnly.FromDateTime(plant.AcquiredDate.Date).DayNumber < CheckupIntervalDays)
        {
            return false;
        }

        return plant.LastCheckupAt is not { } last
            || today.DayNumber - DateOnly.FromDateTime(last.Date).DayNumber >= CheckupIntervalDays;
    }
}
