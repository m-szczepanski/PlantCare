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

    public static bool TryValidate(string? json, out IReadOnlyList<DiagnosisEntry> entries)
    {
        entries = [];

        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<DiagnosisEntry>>(json, JsonOptions);
            if (parsed is null)
            {
                return false;
            }

            foreach (var entry in parsed)
            {
                if (string.IsNullOrWhiteSpace(entry.Symptom) || entry.Causes is null ||
                    entry.Causes.Count == 0 || entry.Causes.Any(string.IsNullOrWhiteSpace))
                {
                    return false;
                }
            }

            entries = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
