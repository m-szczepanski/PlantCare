namespace PlantCare.Api.Models;

public class Plant
{
    public int Id { get; set; }

    public int? PlantProfileId { get; set; }

    public PlantProfile? PlantProfile { get; set; }

    public required string NickName { get; set; }

    public int? RoomId { get; set; }

    public Room? Room { get; set; }

    public string? PhotoUrl { get; set; }

    public int? PotSizeCm { get; set; }

    /// <summary>Substrate category driving the watering-schedule permeability factor.</summary>
    public SoilType? SoilType { get; set; }

    public string? SoilMix { get; set; }

    public string? PropagatedFrom { get; set; }

    /// <summary>When false the plant is skipped by the daily watering digest.</summary>
    public bool NotifyEnabled { get; set; } = true;

    /// <summary>Vacation snooze: reminders are skipped until this instant.</summary>
    public DateTime? SnoozedUntil { get; set; }

    public DateTime AcquiredDate { get; set; }

    public List<CareTask> CareTasks { get; set; } = [];

}
