using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

/// <summary>
/// A pickable soil type with the factor its water permeability applies to the
/// watering interval (&lt;1 = dries faster = water sooner, &gt;1 = holds water =
/// water later) and the well-known substrate blends offered as picker presets
/// for it. The frontend renders the localized label from <see cref="Type"/>.
/// </summary>
public class SoilTypeOptionDto
{
    public required SoilType Type { get; set; }

    public required double WateringIntervalFactor { get; set; }

    public required IReadOnlyList<string> Mixes { get; set; }
}
