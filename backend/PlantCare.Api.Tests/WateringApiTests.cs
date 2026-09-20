using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using Xunit;

namespace PlantCare.Api.Tests;

public class WateringApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WateringApiTests()
    {
        _factory = _database.CreateFactory();
        _client = _factory.CreateClient();
    }

    private static DateTime Today => DateTime.UtcNow.Date;

    [Fact]
    public async Task Water_ValidPlant_UpdatesLastWateredAt_AndResetsDueStatus()
    {
        var plant = await CreatePlant(customWateringIntervalDays: 7, lastWateredAt: Today.AddDays(-10));

        var overdue = await GetPlant(plant.Id);
        Assert.Equal(PlantDueStatus.Overdue, overdue.DueStatus);

        var response = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/water", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var watered = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(watered);
        Assert.NotNull(watered.LastWateredAt);
        Assert.Equal(Today, watered.LastWateredAt!.Value.Date);
        Assert.Equal(PlantDueStatus.Upcoming, watered.DueStatus);
        Assert.Equal(7, watered.DaysUntilDue);

        var fetched = await GetPlant(plant.Id);
        Assert.Equal(watered.LastWateredAt, fetched.LastWateredAt);
    }

    [Fact]
    public async Task Water_AppendsLogRow_VisibleInWateringHistory()
    {
        var plant = await CreatePlant(customWateringIntervalDays: 7);

        var first = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/water", new { note = "Soaked thoroughly" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/water", new { });

        var logs = await _client.GetFromJsonAsync<List<WateringLogResponseDto>>($"/api/plants/{plant.Id}/watering-logs", Options);
        Assert.NotNull(logs);
        Assert.Equal(2, logs.Count);
        Assert.Equal("Soaked thoroughly", logs[1].Note);
        Assert.Null(logs[0].Note);
        Assert.True(logs[0].WateredAt >= logs[1].WateredAt);
    }

    [Fact]
    public async Task Water_UnknownPlant_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/plants/424242/water", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WateringLogs_UnknownPlant_Returns404()
    {
        var response = await _client.GetAsync("/api/plants/424242/watering-logs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Water_WithAmountAndMethod_PersistedInHistory()
    {
        var plant = await CreatePlant(customWateringIntervalDays: 7);

        var response = await _client.PostAsJsonAsync(
            $"/api/plants/{plant.Id}/water",
            new { note = "Evening drink", amountMilliliters = 300, method = "Rainwater" },
            Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await _client.GetFromJsonAsync<List<WateringLogResponseDto>>($"/api/plants/{plant.Id}/watering-logs", Options);
        var log = Assert.Single(logs!);
        Assert.Equal(300, log.AmountMilliliters);
        Assert.Equal(PlantCare.Api.Models.WateringMethod.Rainwater, log.Method);
    }

    [Fact]
    public async Task Water_OverlongNote_Returns400()
    {
        var plant = await CreatePlant(customWateringIntervalDays: 7);

        var response = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/water", new { note = new string('x', 501) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UndoWater_RemovesLatestLog_AndRewindsLastWateredAt()
    {
        var plant = await CreatePlant(customWateringIntervalDays: 7);
        await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/water", new { note = "first" });
        await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/water", new { note = "second" });

        var response = await _client.DeleteAsync($"/api/plants/{plant.Id}/water");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var undone = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(undone);
        var logs = await _client.GetFromJsonAsync<List<WateringLogResponseDto>>($"/api/plants/{plant.Id}/watering-logs", Options);
        Assert.NotNull(logs);
        Assert.Single(logs);
        Assert.Equal("first", logs[0].Note);
        Assert.Equal(logs[0].WateredAt, undone.LastWateredAt);
    }

    [Fact]
    public async Task UndoWater_LastLog_RemovesIt_AndClearsLastWateredAt()
    {
        var plant = await CreatePlant(customWateringIntervalDays: 7);
        await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/water", new { });

        var response = await _client.DeleteAsync($"/api/plants/{plant.Id}/water");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var undone = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.Null(undone!.LastWateredAt);

        var logs = await _client.GetFromJsonAsync<List<WateringLogResponseDto>>($"/api/plants/{plant.Id}/watering-logs", Options);
        Assert.Empty(logs!);
    }

    [Fact]
    public async Task UndoWater_NoLogs_IsNoOp()
    {
        var plant = await CreatePlant(customWateringIntervalDays: 7, lastWateredAt: Today.AddDays(-2));

        var response = await _client.DeleteAsync($"/api/plants/{plant.Id}/water");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var undone = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.Equal(Today.AddDays(-2), undone!.LastWateredAt!.Value.Date);
    }

    [Fact]
    public async Task UndoWater_UnknownPlant_Returns404()
    {
        var response = await _client.DeleteAsync("/api/plants/424242/water");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<PlantResponseDto> CreatePlant(int? customWateringIntervalDays = null, DateTime? lastWateredAt = null)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Thirsty Terry",
            location = "Kitchen",
            customWateringIntervalDays,
            lastWateredAt,
        }, Options);

        var plant = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(plant);
        return plant;
    }

    private async Task<PlantResponseDto> GetPlant(int id)
    {
        var plant = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{id}", Options);
        Assert.NotNull(plant);
        return plant;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
