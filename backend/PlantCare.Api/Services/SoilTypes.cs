using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public readonly record struct SoilTypeInfo(SoilType Type, double WateringIntervalFactor, IReadOnlyList<string> Mixes);

/// <summary>
/// Permeability-driven watering factors for each soil type — the single source
/// of truth for how the substrate scales a plant's base watering interval.
/// A factor below 1 means the mix drains and dries faster than the all-purpose
/// baseline (water sooner); above 1 means it holds water longer (water later).
/// Each type also lists well-known substrate blends ("mixes") that map onto it,
/// so the plant-form picker can offer the old soil-mix catalog names without a
/// second field. The frontend mirrors these values in <c>src/lib/soilTypes.ts</c>
/// for the live next-due preview; keep both in sync.
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

    private static readonly Dictionary<SoilType, IReadOnlyList<string>> MixesByType = new()
    {
        [SoilType.AllPurpose] = ["All-purpose potting mix", "Worm casting boost", "Leaf mold & loam"],
        [SoilType.CactusMix] = ["Cactus & succulent mix", "Pumice-heavy inorganic mix"],
        [SoilType.ChunkyBark] = ["Aroid chunky blend", "Orchid bark mix"],
        [SoilType.PeatCoco] = ["Peat & perlite mix", "Coco coir & perlite blend", "Sphagnum moss"],
        [SoilType.SemiHydro] = ["Semi-hydro LECA"],
        [SoilType.SelfWatering] = ["Self-watering pot blend"],
    };

    /// <summary>Legacy soil-mix catalog name → permeability category (migration/import helper).</summary>
    public static readonly IReadOnlyDictionary<string, SoilType> MixToType = MixesByType
        .SelectMany(kv => kv.Value.Select(mix => (mix, kv.Key)))
        .ToDictionary(x => x.mix, x => x.Key, StringComparer.Ordinal);

    public static IReadOnlyList<SoilTypeInfo> All { get; } = Factors
        .Select(kv => new SoilTypeInfo(kv.Key, kv.Value, MixesByType[kv.Key]))
        .OrderBy(i => i.Type)
        .ToList();

    public static double Factor(SoilType? soil) => soil is { } s ? Factors[s] : 1.0;

    /// <summary>Applies the substrate factor to a base interval, never below one day.</summary>
    public static int AdjustInterval(SoilType? soil, int baseIntervalDays)
        => Math.Max(1, (int)Math.Round(baseIntervalDays * Factor(soil), MidpointRounding.AwayFromZero));
}
