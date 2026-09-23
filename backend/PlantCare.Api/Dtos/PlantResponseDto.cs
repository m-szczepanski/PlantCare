using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class PlantResponseDto
{
    public int Id { get; set; }

    public required string NickName { get; set; }

    public int? RoomId { get; set; }

    public string? RoomName { get; set; }

    public string? PhotoUrl { get; set; }

    public int? PotSizeCm { get; set; }

    public SoilType? SoilType { get; set; }

    public string? SoilMix { get; set; }

    public string? PropagatedFrom { get; set; }

    public bool NotifyEnabled { get; set; }

    public DateTime? SnoozedUntil { get; set; }

    public DateTime? SoilWetUntil { get; set; }

    public DateTime AcquiredDate { get; set; }

    public int? PlantProfileId { get; set; }

    public string? ProfileCommonName { get; set; }

    public bool ProfileToxicToPets { get; set; }

    public bool ProfileToxicToChildren { get; set; }

    public PlantCareTipsDto? CareTips { get; set; }

    public int? CustomWateringIntervalDays { get; set; }

    public bool? ReduceInWinter { get; set; }

    public DateTime? LastWateredAt { get; set; }

    public PlantDueStatus DueStatus { get; set; }

    public int? WateringIntervalDays { get; set; }

    public int? DaysUntilDue { get; set; }

    public DateTime? NextDueDate { get; set; }

    public required string DueMessage { get; set; }

    /// <summary>
    /// Plant profile light need vs the room's measured light exposure;
    /// null when either side is unknown.
    /// </summary>
    public RoomLightMatch? RoomLightMatch { get; set; }

    /// <summary>Health status from the latest monthly checkup (null = never checked).</summary>
    public HealthStatus? HealthStatus { get; set; }

    public DateTime? LastCheckupAt { get; set; }

    /// <summary>True when the plant is old enough and has no checkup answer in the current period.</summary>
    public bool CheckupDue { get; set; }
}
