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

    public string? PropagatedFrom { get; set; }

    /// <summary>When false the plant is skipped by the daily watering digest.</summary>
    public bool NotifyEnabled { get; set; } = true;

    /// <summary>Vacation snooze: reminders are skipped until this instant.</summary>
    public DateTime? SnoozedUntil { get; set; }

    /// <summary>
    /// Soil-wet deferral: the watering is not needed yet, so the due date is
    /// pushed out to this instant (the plant re-presents as due once it passes).
    /// </summary>
    public DateTime? SoilWetUntil { get; set; }

    public DateTime AcquiredDate { get; set; }

    /// <summary>Current health from the latest checkup (null = never checked); see <c>HealthPolicy</c>.</summary>
    public HealthStatus? HealthStatus { get; set; }

    /// <summary>When the latest health checkup was answered.</summary>
    public DateTime? LastCheckupAt { get; set; }

    /// <summary>When the ntfy checkup-due reminder was last sent for this plant.</summary>
    public DateTime? CheckupReminderSentAt { get; set; }

    public List<CareTask> CareTasks { get; set; } = [];

    public List<PlantHealthCheck> HealthChecks { get; set; } = [];

}
