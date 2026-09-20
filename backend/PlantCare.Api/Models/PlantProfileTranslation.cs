namespace PlantCare.Api.Models;

/// <summary>
/// Per-language localized content for a <see cref="PlantProfile"/> (species name,
/// humidity notes, care tips, diagnosis checklist). English stays on the profile
/// row itself as the canonical fallback; a missing field (or row) falls back to it.
/// </summary>
public class PlantProfileTranslation
{
    public int Id { get; set; }

    public int PlantProfileId { get; set; }

    /// <summary>BCP-47 two-letter language code, e.g. "pl".</summary>
    public required string Language { get; set; }

    public string? CommonName { get; set; }

    public string? HumidityNotes { get; set; }

    public string? CareTips { get; set; }

    public string? DiagnosisChecklist { get; set; }

    public PlantProfile PlantProfile { get; set; } = null!;
}
