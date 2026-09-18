using System.ComponentModel.DataAnnotations;

namespace PlantCare.Api.Dtos;

public class WaterPlantRequestDto
{
    [StringLength(500)]
    public string? Note { get; set; }
}
