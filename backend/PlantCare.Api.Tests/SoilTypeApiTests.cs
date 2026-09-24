using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using Xunit;

namespace PlantCare.Api.Tests;

public class SoilTypeApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SoilTypeApiTests()
    {
        _factory = _database.CreateFactory();
        _client = _factory.CreateClient();
    }

    private static DateTime Today => DateTime.UtcNow.Date;

    [Fact]
    public async Task Create_WithSoilType_PersistsAndAdjustsEffectiveInterval()
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Semi-hydro pothos",
            soilType = SoilType.SemiHydro,
            customWateringIntervalDays = 10,
            lastWateredAt = Today,
        }, Options);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.Equal(SoilType.SemiHydro, body!.SoilType);
        // 10-day base × 1.3 semi-hydro factor → 13 days effective.
        Assert.Equal(13, body.WateringIntervalDays);
        Assert.Equal(PlantDueStatus.Upcoming, body.DueStatus);
    }

    [Fact]
    public async Task Create_WithoutSoilType_DefaultsToNull_AndKeepsInterval()
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Legacy plant",
            customWateringIntervalDays = 10,
            lastWateredAt = Today,
        }, Options);
        var body = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.Null(body!.SoilType);
        Assert.Equal(10, body.WateringIntervalDays);
    }

    [Fact]
    public async Task Create_InvalidSoilType_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Bad soil",
            soilType = "MoonDust",
        }, Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesSoilType_AndCanClearIt()
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Switch substrate",
            soilType = SoilType.SemiHydro,
            customWateringIntervalDays = 10,
            lastWateredAt = Today,
        }, Options);
        var body = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        var update = await _client.PutAsJsonAsync($"/api/plants/{body!.Id}", new
        {
            nickName = "Switch substrate",
            soilType = SoilType.ChunkyBark,
            customWateringIntervalDays = 10,
            lastWateredAt = Today,
        }, Options);
        var changed = await update.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.Equal(SoilType.ChunkyBark, changed!.SoilType);
        Assert.Equal(6, changed.WateringIntervalDays);

        var cleared = await _client.PutAsJsonAsync($"/api/plants/{body.Id}", new
        {
            nickName = "Switch substrate",
            soilType = null as SoilType?,
            customWateringIntervalDays = 10,
            lastWateredAt = Today,
        }, Options);
        var clearedBody = await cleared.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.Null(clearedBody!.SoilType);
        Assert.Equal(10, clearedBody.WateringIntervalDays);
    }

    [Fact]
    public async Task ReferenceData_SoilTypes_CoverEveryLegacySoilMixName()
    {
        var options = await _client.GetFromJsonAsync<List<SoilTypeOptionDto>>(
            "/api/reference-data/soil-types", Options);

        Assert.NotNull(options);
        Assert.Equal(6, options.Count);

        var mixes = options.SelectMany(o => o.Mixes).ToList();
        foreach (var legacyName in new[]
        {
            "All-purpose potting mix",
            "Aroid chunky blend",
            "Cactus & succulent mix",
            "Orchid bark mix",
            "Peat & perlite mix",
            "Coco coir & perlite blend",
            "Worm casting boost",
            "Pumice-heavy inorganic mix",
            "Sphagnum moss",
            "Leaf mold & loam",
            "Semi-hydro LECA",
            "Self-watering pot blend",
        })
        {
            Assert.Contains(legacyName, mixes);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
