namespace PlantCare.Api.Models;

/// <summary>
/// One row per sent daily digest; used for same-day dedup and audit.
/// Replaced the old per-plant <c>NotificationLog</c> table.
/// </summary>
public class NotificationDigest
{
    public int Id { get; set; }

    public DateTime SentAt { get; set; }

    public int PlantCount { get; set; }

    public int OverdueCount { get; set; }

    public int Priority { get; set; }
}
