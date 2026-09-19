using System.ComponentModel.DataAnnotations;
using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class RoomRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public required string Name { get; set; }

    public RoomOrientation? Orientation { get; set; }

    public LightRequirement? LightExposure { get; set; }

    public HumidityLevel? Humidity { get; set; }

    [Range(-10, 45)]
    public int? TemperatureCelsius { get; set; }
}
