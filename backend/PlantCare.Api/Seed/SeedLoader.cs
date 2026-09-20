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
                DiagnosisChecklist = seed.DiagnosisChecklist,
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
        [property: JsonPropertyName("toxicToChildren")] bool ToxicToChildren = false,
        [property: JsonPropertyName("diagnosisChecklist")] string? DiagnosisChecklist = null);

    /// <summary>
    /// Localized profile content shipped as plant-profiles.&lt;lang&gt;.json next to the
    /// canonical seed file. Entries are keyed by the English common name; existing
    /// translations are refreshed (seed is the source of truth for shipped languages),
    /// entries for unknown profiles are skipped.
    /// </summary>
    public static async Task LoadProfileTranslationsAsync(
        AppDbContext db,
        string seedDirectory,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        foreach (var file in Directory.EnumerateFiles(seedDirectory, "plant-profiles.*.json").Order(StringComparer.Ordinal))
        {
            var language = Path.GetFileName(file)["plant-profiles.".Length..^".json".Length];
            if (language.Length != 2 || !Services.Localization.Messages.IsSupported(language))
            {
                continue;
            }

            try
            {
                await LoadTranslationFileAsync(db, file, language, logger, cancellationToken);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Skipping invalid translation seed file {File}.", file);
            }
        }
    }

    private static async Task LoadTranslationFileAsync(
        AppDbContext db,
        string path,
        string language,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var entries = await JsonSerializer.DeserializeAsync<List<SeedProfileTranslation>>(stream, JsonOptions, cancellationToken)
            ?? [];

        var profiles = await db.PlantProfiles
            .Include(p => p.Translations)
            .ToListAsync(cancellationToken);
        var byName = profiles.ToDictionary(p => p.CommonName, StringComparer.OrdinalIgnoreCase);

        var upserted = 0;
        foreach (var entry in entries)
        {
            if (!byName.TryGetValue(entry.Profile, out var profile))
            {
                logger.LogDebug("Translation entry for unknown profile {Profile} in {File}; skipping.", entry.Profile, path);
                continue;
            }

            var translation = profile.Translations.FirstOrDefault(t =>
                string.Equals(t.Language, language, StringComparison.OrdinalIgnoreCase));
            if (translation is null)
            {
                translation = new PlantProfileTranslation { PlantProfileId = profile.Id, Language = language };
                db.PlantProfileTranslations.Add(translation);
                profile.Translations.Add(translation);
            }

            translation.CommonName = entry.CommonName;
            translation.HumidityNotes = entry.HumidityNotes;
            translation.CareTips = entry.CareTips;
            translation.DiagnosisChecklist = entry.DiagnosisChecklist;
            upserted++;
        }

        if (upserted > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} {Language} profile translation(s) from {File}.", upserted, language, path);
        }
    }

    private sealed record SeedProfileTranslation(
        [property: JsonPropertyName("profile")] string Profile,
        [property: JsonPropertyName("commonName")] string? CommonName,
        [property: JsonPropertyName("humidityNotes")] string? HumidityNotes,
        [property: JsonPropertyName("careTips")] string? CareTips,
        [property: JsonPropertyName("diagnosisChecklist")] string? DiagnosisChecklist = null);
}
