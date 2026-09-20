using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class RoomResponseDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public RoomOrientation? Orientation { get; set; }

    public LightRequirement? LightExposure { get; set; }

    public HumidityLevel? Humidity { get; set; }

    public int? TemperatureCelsius { get; set; }

    public required int PlantCount { get; set; }
}
