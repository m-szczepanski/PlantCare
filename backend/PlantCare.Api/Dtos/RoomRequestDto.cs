using System.ComponentModel.DataAnnotations;
using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class RoomRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public required string Name { get; set; }

    public RoomOrientation? Orientation { get; set; }
}
