namespace PlantCare.Api.Models;

/// <summary>
/// Catalog of substrate mixes a plant can be potted in. Plants keep referencing
/// the mix by name (Plant.SoilMix stays a plain string) so the catalog guides the
/// picker without breaking legacy free-text values or import documents.
/// </summary>
public class SoilMix
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
