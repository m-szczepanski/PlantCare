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

    public string? SoilMix { get; set; }

    public string? PropagatedFrom { get; set; }

    public DateTime AcquiredDate { get; set; }

    public List<CareTask> CareTasks { get; set; } = [];

    public List<NotificationLog> NotificationLogs { get; set; } = [];
}
