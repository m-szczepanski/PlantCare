namespace PlantCare.Api.Dtos;

public class PlantProfileResponseDto
{
    public int Id { get; set; }

    public required string CommonName { get; set; }

    public string? ScientificName { get; set; }

    public int DefaultWateringIntervalDays { get; set; }
}
