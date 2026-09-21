namespace PlantCare.Api.Dtos;

/// <summary>
/// A soil mix from the DB-backed catalog that powers the plant-form picker.
/// The value stored on a plant is the mix <see cref="Name"/> (plain string),
/// so legacy free-text values and import documents stay valid.
/// </summary>
public class SoilMixOptionDto
{
    public required int Id { get; set; }

    public required string Name { get; set; }
}
