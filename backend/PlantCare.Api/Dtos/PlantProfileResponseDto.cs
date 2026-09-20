using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class PlantProfileResponseDto
{
    public int Id { get; set; }

    public required string CommonName { get; set; }

    public string? ScientificName { get; set; }

    public int DefaultWateringIntervalDays { get; set; }

    public LightRequirement LightRequirement { get; set; }

    public required string HumidityNotes { get; set; }

    public required string CareTips { get; set; }

    public required int PlantCount { get; set; }
}
