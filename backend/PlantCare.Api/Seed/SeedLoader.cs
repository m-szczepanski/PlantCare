using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Models;

namespace PlantCare.Api.Seed;

public static class SeedLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task LoadPlantProfilesAsync(
        AppDbContext db,
        string seedFilePath,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(seedFilePath))
        {
            logger.LogWarning("Seed file {SeedFilePath} not found; skipping plant profile seeding.", seedFilePath);
            return;
        }

        await LoadProfileFileAsync(db, seedFilePath, logger, cancellationToken);
    }

    /// <summary>
    /// Drop-in user species: every *.json file under <paramref name="directory"/> is
    /// loaded at startup (arrays shaped like plant-profiles.json). Existing names are
    /// skipped, malformed files are logged and ignored — a bad custom file must never
    /// block startup.
    /// </summary>
    public static async Task LoadCustomProfilesAsync(
        AppDbContext db,
        string? directory,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal))
        {
            try
            {
                await LoadProfileFileAsync(db, file, logger, cancellationToken);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Skipping invalid custom seed file {File}.", file);
            }
        }
    }

    private static async Task LoadProfileFileAsync(
        AppDbContext db,
        string path,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var seedProfiles = await JsonSerializer.DeserializeAsync<List<SeedPlantProfile>>(stream, JsonOptions, cancellationToken)
            ?? [];

        var existingNames = await db.PlantProfiles
            .Select(p => p.CommonName)
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var seed in seedProfiles)
        {
            if (!existingSet.Add(seed.CommonName))
            {
                continue;
            }

            db.PlantProfiles.Add(new PlantProfile
            {
                CommonName = seed.CommonName,
                ScientificName = seed.ScientificName,
                DefaultWateringIntervalDays = seed.DefaultWateringIntervalDays,
                LightRequirement = seed.LightRequirement,
                HumidityNotes = seed.HumidityNotes,
                CareTips = seed.CareTips,
                ToxicToPets = seed.ToxicToPets,
                ToxicToChildren = seed.ToxicToChildren,
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {AddedCount} plant profile(s) from {SeedFilePath}.", added, path);
        }
    }

    private sealed record SeedPlantProfile(
        [property: JsonPropertyName("commonName")] string CommonName,
        [property: JsonPropertyName("scientificName")] string? ScientificName,
        [property: JsonPropertyName("defaultWateringIntervalDays")] int DefaultWateringIntervalDays,
        [property: JsonPropertyName("lightRequirement")] LightRequirement LightRequirement,
        [property: JsonPropertyName("humidityNotes")] string HumidityNotes,
        [property: JsonPropertyName("careTips")] string CareTips,
        [property: JsonPropertyName("toxicToPets")] bool ToxicToPets = false,
        [property: JsonPropertyName("toxicToChildren")] bool ToxicToChildren = false);
}
