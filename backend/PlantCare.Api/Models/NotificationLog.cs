namespace PlantCare.Api.Models;

public class NotificationLog
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public NotificationType Type { get; set; }
}
