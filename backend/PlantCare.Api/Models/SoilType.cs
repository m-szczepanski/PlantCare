namespace PlantCare.Api.Models;

/// <summary>
/// The substrate a plant is potted in. Each type has a different water
/// permeability, so it scales the base watering interval (fast-draining
/// mixes dry sooner and need more frequent watering, retentive/reservoir
/// mixes less). See <see cref="Services.SoilTypes"/> for the factors.
/// </summary>
public enum SoilType
{
    AllPurpose,
    CactusMix,
    ChunkyBark,
    PeatCoco,
    SemiHydro,
    SelfWatering,
}
