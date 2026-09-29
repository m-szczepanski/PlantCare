using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public readonly record struct SoilTypeInfo(SoilType Type, double WateringIntervalFactor, IReadOnlyList<string> Mixes, bool TopUpEligible);

/// <summary>
/// Permeability-driven watering factors for each soil type — the single source
/// of truth for how the substrate scales a plant's base watering interval.
/// A factor above 1 means the mix holds water longer (water later); mixes that
/// drain and dry faster than the all-purpose baseline (below 1) keep the base
/// interval as-is and instead get the mid-cycle "top up water" prompt (see
/// <see cref="TopUpWatering"/> and <see cref="WateringScheduleService.GetTopUpDueInfo"/>) —
/// topping up is their answer to fast drainage, not a shortened cycle.
/// Each type also lists well-known substrate blends ("mixes") that map onto it,
/// so the plant-form picker can offer the old soil-mix catalog names without a
/// second field. The frontend mirrors these values in <c>src/lib/soilTypes.ts</c>
/// for the live next-due preview; keep both in sync.
/// </summary>
public static class SoilTypes
{
    /// <summary>Raw drainage ratios; permeable entries are the "top up instead of shortening" set.</summary>
    private static readonly Dictionary<SoilType, double> Drainage = new()
    {
        [SoilType.AllPurpose] = 1.0,
        [SoilType.CactusMix] = 0.7,
        [SoilType.ChunkyBark] = 0.55,
        [SoilType.PeatCoco] = 1.15,
        [SoilType.SemiHydro] = 1.3,
        [SoilType.SelfWatering] = 1.5,
    };

    private static readonly HashSet<SoilType> PermeableSoils = Drainage.Where(kv => kv.Value < 1.0).Select(kv => kv.Key).ToHashSet();

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

    public static IReadOnlyList<SoilTypeInfo> All { get; } = Drainage
        .Select(kv => new SoilTypeInfo(kv.Key, Factor(kv.Key), MixesByType[kv.Key], PermeableSoils.Contains(kv.Key)))
        .OrderBy(i => i.Type)
        .ToList();

    /// <summary>
    /// The factor applied to the watering interval: retentive mixes scale it up,
    /// fast-draining mixes stay at the baseline (1.0) because their extra water
    /// comes from the derived top-up mid-cycle instead.
    /// </summary>
    public static double Factor(SoilType? soil)
        => soil is { } s && !PermeableSoils.Contains(s) ? Drainage[s] : 1.0;

    /// <summary>
    /// Highly permeable mixes drain so fast that between two full waterings the
    /// top layer dries out; those soils opt the plant into the mid-cycle
    /// "top up water" prompt (see <see cref="TopUpWatering"/> and
    /// <see cref="WateringScheduleService.GetTopUpDueInfo"/>). Anything as slow
    /// as or slower than the all-purpose baseline does not need top-ups.
    /// </summary>
    public static bool IsHighlyPermeable(SoilType? soil) => soil is { } s && PermeableSoils.Contains(s);

    /// <summary>Applies the substrate factor to a base interval, never below one day.</summary>
    public static int AdjustInterval(SoilType? soil, int baseIntervalDays)
        => Math.Max(1, (int)Math.Round(baseIntervalDays * Factor(soil), MidpointRounding.AwayFromZero));
}
