namespace PlantCare.Api.Models;

public class PlantNote
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public required string Text { get; set; }

    /// <summary>Pinned notes surface above the regular notes list on the plant detail page.</summary>
    public bool IsPinned { get; set; }
}
