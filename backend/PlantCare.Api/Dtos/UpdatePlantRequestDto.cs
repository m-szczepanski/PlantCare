using System.ComponentModel.DataAnnotations;
using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class UpdatePlantRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public required string NickName { get; set; }

    public int? RoomId { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [Range(1, 200)]
    public int? PotSizeCm { get; set; }

    public SoilType? SoilType { get; set; }

    [StringLength(200)]
    public string? SoilMix { get; set; }

    [StringLength(200)]
    public string? PropagatedFrom { get; set; }

    public DateTime AcquiredDate { get; set; } = DateTime.UtcNow;

    [Range(1, 3650)]
    public int? CustomWateringIntervalDays { get; set; }

    public bool? ReduceInWinter { get; set; }

    public int? PlantProfileId { get; set; }

    public bool NotifyEnabled { get; set; } = true;

    public DateTime? LastWateredAt { get; set; }
}
