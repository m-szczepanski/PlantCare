using System.ComponentModel.DataAnnotations;
using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class WaterPlantRequestDto
{
    [StringLength(500)]
    public string? Note { get; set; }

    [Range(1, 100_000)]
    public int? AmountMilliliters { get; set; }

    public WateringMethod? Method { get; set; }
}
