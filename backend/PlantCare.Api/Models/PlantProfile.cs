namespace PlantCare.Api.Models;

public class PlantProfile
{
    public int Id { get; set; }

    public required string CommonName { get; set; }

    public string? ScientificName { get; set; }

    public int DefaultWateringIntervalDays { get; set; }

    public LightRequirement LightRequirement { get; set; }

    public required string HumidityNotes { get; set; }

    public required string CareTips { get; set; }

    public bool DefaultReduceInWinter { get; set; }

    public bool ToxicToPets { get; set; }

    public bool ToxicToChildren { get; set; }

    /// <summary>
    /// JSON array of {symptom, causes[]} entries rendered as a per-species
    /// diagnostics checklist on the plant detail; validated on write.
    /// </summary>
    public string? DiagnosisChecklist { get; set; }

    public List<Plant> Plants { get; set; } = [];

    public List<PlantProfileTranslation> Translations { get; set; } = [];
}
