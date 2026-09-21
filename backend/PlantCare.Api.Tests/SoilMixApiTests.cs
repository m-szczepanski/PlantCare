using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Seed;
using Xunit;

namespace PlantCare.Api.Tests;

public class SoilMixApiTests : IDisposable
{
    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public SoilMixApiTests()
    {
        _client = _database.CreateFactory().CreateClient();
    }

    [Fact]
    public async Task GetSoilMixes_ReturnsSeededCatalog_SortedByName()
    {
        var mixes = await _client.GetFromJsonAsync<List<SoilMixOptionDto>>("/api/reference-data/soil-mixes");

        Assert.NotNull(mixes);
        Assert.NotEmpty(mixes);
        Assert.Contains("Cactus & succulent mix", mixes.Select(m => m.Name));

        var names = mixes.Select(m => m.Name).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal).ToList(), names);
    }

    [Fact]
    public async Task LoadSoilMixes_IsIdempotent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_database.ConnectionString)
            .Options;
        await using var db = new AppDbContext(options);

        var seedFile = Path.Combine(AppContext.BaseDirectory, "Seed", "soil-mixes.json");
        Assert.True(File.Exists(seedFile), "soil-mixes.json must ship with the build output");

        var before = await db.SoilMixes.CountAsync();
        Assert.True(before > 0, "startup seeding should insert the catalog");

        await SeedLoader.LoadSoilMixesAsync(db, seedFile, NullLogger.Instance);

        Assert.Equal(before, await db.SoilMixes.CountAsync());
    }

    public void Dispose() => _database.Dispose();
}
