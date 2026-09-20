using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class PlantResponseDto
{
    public int Id { get; set; }

    public required string NickName { get; set; }

    public int? RoomId { get; set; }

    public string? RoomName { get; set; }

    public string? PhotoUrl { get; set; }

    public DateTime AcquiredDate { get; set; }

    public int? PlantProfileId { get; set; }

    public string? ProfileCommonName { get; set; }

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
}
