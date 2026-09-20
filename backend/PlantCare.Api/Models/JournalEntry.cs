namespace PlantCare.Api.Models;

/// <summary>
/// Dated photo + note growth log entry per plant (distinct from watering notes).
/// </summary>
public class JournalEntry
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    public DateTime EntryDate { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Text { get; set; }
}
