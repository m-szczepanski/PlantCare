using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using Xunit;

namespace PlantCare.Api.Tests;

public class CustomSeedTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly string _customDir = Path.Combine(Path.GetTempPath(), $"plantcare-seed-{Guid.NewGuid():N}");

    [Fact]
    public async Task Startup_LoadsCustomSeedFiles_AndIgnoresBrokenOnes()
    {
        Directory.CreateDirectory(_customDir);
        await File.WriteAllTextAsync(
            Path.Combine(_customDir, "a-fern.json"),
            """
            [
              {
                "commonName": "User Drop-in Fern",
                "scientificName": "Nephrolepis exotica",
                "defaultWateringIntervalDays": 4,
                "lightRequirement": "Medium",
                "humidityNotes": "Loves bathroom shelves.",
                "careTips": "Keep evenly moist and humid."
              }
            ]
            """);
        await File.WriteAllTextAsync(Path.Combine(_customDir, "broken.json"), "{ not json");

        using var factory = _database.CreateFactory(configureBuilder: builder =>
            builder.UseSetting("SEED_CUSTOM_PATH", _customDir));
        var client = factory.CreateClient();

        var profiles = await client.GetFromJsonAsync<List<PlantProfileResponseDto>>("/api/plant-profiles", Options);

        Assert.NotNull(profiles);
        Assert.Contains(profiles, p => p.CommonName == "User Drop-in Fern");
        // The bundled seed still loaded despite the malformed custom file.
        Assert.Contains(profiles, p => p.CommonName == "Monstera");

        // Startup ran once per factory; a second factory over the same DB must not duplicate.
        using var secondFactory = _database.CreateFactory(configureBuilder: builder =>
            builder.UseSetting("SEED_CUSTOM_PATH", _customDir));
        var after = await secondFactory.CreateClient()
            .GetFromJsonAsync<List<PlantProfileResponseDto>>("/api/plant-profiles", Options);
        Assert.Single(after!, p => p.CommonName == "User Drop-in Fern");
    }

    public void Dispose()
    {
        _database.Dispose();
        try
        {
            Directory.Delete(_customDir, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
