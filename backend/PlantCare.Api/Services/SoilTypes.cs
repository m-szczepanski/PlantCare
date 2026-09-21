using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public readonly record struct SoilTypeInfo(SoilType Type, double WateringIntervalFactor);

/// <summary>
/// Permeability-driven watering factors for each soil type — the single source
/// of truth for how the substrate scales a plant's base watering interval.
/// A factor below 1 means the mix drains and dries faster than the all-purpose
/// baseline (water sooner); above 1 means it holds water longer (water later).
/// The frontend mirrors these values in <c>src/lib/soilTypes.ts</c> for the
/// live next-due preview; keep both in sync.
/// </summary>
public static class SoilTypes
{
    private static readonly Dictionary<SoilType, double> Factors = new()
    {
        [SoilType.AllPurpose] = 1.0,
        [SoilType.CactusMix] = 0.7,
        [SoilType.ChunkyBark] = 0.55,
        [SoilType.PeatCoco] = 1.15,
        [SoilType.SemiHydro] = 1.3,
        [SoilType.SelfWatering] = 1.5,
    };

    public static IReadOnlyList<SoilTypeInfo> All { get; } = Factors
        .Select(kv => new SoilTypeInfo(kv.Key, kv.Value))
        .OrderBy(i => i.Type)
        .ToList();

    public static double Factor(SoilType? soil) => soil is { } s ? Factors[s] : 1.0;

    /// <summary>Applies the substrate factor to a base interval, never below one day.</summary>
    public static int AdjustInterval(SoilType? soil, int baseIntervalDays)
        => Math.Max(1, (int)Math.Round(baseIntervalDays * Factor(soil), MidpointRounding.AwayFromZero));
}
