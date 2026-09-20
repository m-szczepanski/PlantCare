using System.ComponentModel.DataAnnotations;

namespace PlantCare.Api.Dtos;

public class UpdatePlantRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public required string NickName { get; set; }

    public int? RoomId { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    public DateTime AcquiredDate { get; set; } = DateTime.UtcNow;

    [Range(1, 3650)]
    public int? CustomWateringIntervalDays { get; set; }

    public int? PlantProfileId { get; set; }

    public DateTime? LastWateredAt { get; set; }
}
