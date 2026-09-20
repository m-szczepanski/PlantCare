using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using Xunit;

namespace PlantCare.Api.Tests;

public class QuickActionApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private const string Secret = "test-trigger-secret";

    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public QuickActionApiTests()
    {
        _factory = _database.CreateFactory(configureBuilder: builder =>
        {
            builder.UseSetting("QUICK_ACTION_SECRET", Secret);
            builder.UseSetting("QUICK_ACTION_URL_BASE", "http://plant.lan:3000");
        });
        _client = _factory.CreateClient();
    }

    private async Task<PlantResponseDto> CreateDuePlant(string nickName = "Trigger Terry")
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            customWateringIntervalDays = 7,
            lastWateredAt = DateTime.UtcNow.Date.AddDays(-10),
        }, Options);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    [Fact]
    public async Task QuickWater_WithValidKey_WatersThePlant()
    {
        var plant = await CreateDuePlant();

        var response = await _client.PostAsync($"/api/plants/{plant.Id}/quick-water?key={Secret}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Trigger Terry", await response.Content.ReadAsStringAsync());

        var fetched = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plant.Id}", Options);
        Assert.NotNull(fetched!.LastWateredAt);
    }

    [Fact]
    public async Task QuickWater_GetAlsoWorks()
    {
        var plant = await CreateDuePlant();

        var response = await _client.GetAsync($"/api/plants/{plant.Id}/quick-water?key={Secret}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task QuickWater_BadOrMissingKey_Forbidden()
    {
        var plant = await CreateDuePlant();

        var wrong = await _client.PostAsync($"/api/plants/{plant.Id}/quick-water?key=nope", null);
        var missing = await _client.PostAsync($"/api/plants/{plant.Id}/quick-water", null);

        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, missing.StatusCode);
    }

    [Fact]
    public async Task QuickWater_UnknownPlant_NotFound()
    {
        var response = await _client.PostAsync("/api/plants/424242/quick-water?key=" + Secret, null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task QuickWater_RateLimitedAfterRepeatedAttempts()
    {
        var plant = await CreateDuePlant();

        for (var i = 0; i < 10; i += 1)
        {
            var ok = await _client.PostAsync($"/api/plants/{plant.Id}/quick-water?key=nope", null);
            Assert.Equal(HttpStatusCode.Forbidden, ok.StatusCode);
        }

        var limited = await _client.PostAsync($"/api/plants/{plant.Id}/quick-water?key={Secret}", null);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    [Fact]
    public async Task QuickWater_FeatureDisabled_Returns404()
    {
        using var disabledFactory = _database.CreateFactory();
        var disabledClient = disabledFactory.CreateClient();

        var created = await disabledClient.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Untouchable",
            customWateringIntervalDays = 7,
            lastWateredAt = DateTime.UtcNow.Date.AddDays(-10),
        }, Options);
        var plant = (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;

        var response = await disabledClient.PostAsync($"/api/plants/{plant.Id}/quick-water?key={Secret}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
