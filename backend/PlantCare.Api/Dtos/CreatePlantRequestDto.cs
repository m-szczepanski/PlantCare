using System.ComponentModel.DataAnnotations;

namespace PlantCare.Api.Dtos;

public class CreatePlantRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public required string NickName { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 1)]
    public required string Location { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    public DateTime AcquiredDate { get; set; } = DateTime.UtcNow;

    [Range(1, 3650)]
    public int? CustomWateringIntervalDays { get; set; }

    public int? PlantProfileId { get; set; }

    public DateTime? LastWateredAt { get; set; }
}
