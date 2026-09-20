using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlantCare.Api.Services;

public sealed record DiagnosisEntry(
    [property: JsonPropertyName("symptom")] string Symptom,
    [property: JsonPropertyName("causes")] IReadOnlyList<string> Causes);

public static class DiagnosisChecklist
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static bool TryValidate(string? json, out IReadOnlyList<DiagnosisEntry> entries, out string? error)
    {
        entries = [];
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<DiagnosisEntry>>(json, JsonOptions);
            if (parsed is null)
            {
                error = "The diagnosis checklist must be a JSON array.";
                return false;
            }

            foreach (var entry in parsed)
            {
                if (string.IsNullOrWhiteSpace(entry.Symptom) || entry.Causes is null ||
                    entry.Causes.Count == 0 || entry.Causes.Any(string.IsNullOrWhiteSpace))
                {
                    error = "Every checklist entry needs a symptom and at least one cause.";
                    return false;
                }
            }

            entries = parsed;
            return true;
        }
        catch (JsonException ex)
        {
            error = $"The diagnosis checklist is not valid JSON: {ex.Message}";
            return false;
        }
    }
}
