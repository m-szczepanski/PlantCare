namespace PlantCare.Api.Models;

public class PlantProfile
{
    public int Id { get; set; }

    public required string CommonName { get; set; }

    public string? ScientificName { get; set; }

    public int DefaultWateringIntervalDays { get; set; }

    public LightRequirement LightRequirement { get; set; }

    public required string HumidityNotes { get; set; }

    public required string CareTips { get; set; }

    public List<Plant> Plants { get; set; } = [];
}
