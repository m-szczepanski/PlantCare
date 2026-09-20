using System.ComponentModel.DataAnnotations;
using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class PlantProfileRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public required string CommonName { get; set; }

    [StringLength(200)]
    public string? ScientificName { get; set; }

    [Range(1, 3650)]
    public int DefaultWateringIntervalDays { get; set; } = 7;

    [Range(0, 3)]
    public LightRequirement LightRequirement { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public required string HumidityNotes { get; set; }

    [Required]
    [StringLength(10000, MinimumLength = 1)]
    public required string CareTips { get; set; }

    public bool ToxicToPets { get; set; }

    public bool ToxicToChildren { get; set; }
}
