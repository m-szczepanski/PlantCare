namespace PlantCare.Api.Models;

public class Plant
{
    public int Id { get; set; }

    public int? PlantProfileId { get; set; }

    public PlantProfile? PlantProfile { get; set; }

    public required string NickName { get; set; }

    public required string Location { get; set; }

    public string? PhotoUrl { get; set; }

    public DateTime AcquiredDate { get; set; }

    public int? CustomWateringIntervalDays { get; set; }

    public DateTime? LastWateredAt { get; set; }

    public List<WateringLog> WateringLogs { get; set; } = [];

    public List<NotificationLog> NotificationLogs { get; set; } = [];
}
