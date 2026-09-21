using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class SoilWetApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private sealed class RecordingPublisher : INtfyPublisher
    {
        public List<string> Messages { get; } = [];

        public Task PublishAsync(string title, string message, int priority = 3, string? clickUrl = null, string? buttonLabel = null, string? buttonUrl = null, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private readonly RecordingPublisher _publisher = new();
    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SoilWetApiTests()
    {
        _factory = _database.CreateFactory(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(_publisher);
        });
        _client = _factory.CreateClient();
    }

    private async Task<PlantResponseDto> CreateOverduePlant(string nickName)
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

    private async Task<WateringCheckResult> RunCheckAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IWateringCheckService>().RunAsync();
    }

    [Fact]
    public async Task SoilWet_DefersDuePlant_AndDigestSkipsIt()
    {
        var plant = await CreateOverduePlant("Waterlogged");
        Assert.Equal(PlantDueStatus.Overdue, plant.DueStatus);

        var response = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/soil-wet", new { days = 4 }, Options);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var deferred = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.NotNull(deferred!.SoilWetUntil);
        Assert.True(deferred.SoilWetUntil > DateTime.UtcNow.AddDays(3));
        Assert.Equal(PlantDueStatus.Upcoming, deferred.DueStatus);

        var check = await RunCheckAsync();
        Assert.Equal(0, check.SentDigests);
        Assert.Empty(_publisher.Messages);
    }

    [Fact]
    public async Task SoilWet_ClearRestoresOverdue_AndDigestSends()
    {
        var plant = await CreateOverduePlant("Drying out");
        await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/soil-wet", new { days = 3 }, Options);

        var clear = await _client.DeleteAsync($"/api/plants/{plant.Id}/soil-wet");
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        var restored = await clear.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.Null(restored!.SoilWetUntil);
        Assert.Equal(PlantDueStatus.Overdue, restored.DueStatus);

        var check = await RunCheckAsync();
        Assert.Equal(1, check.SentDigests);
    }

    [Fact]
    public async Task SoilWet_WateringClearsTheDeferral()
    {
        var plant = await CreateOverduePlant("Water anyway");
        await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/soil-wet", new { days = 5 }, Options);

        var water = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/water", new { }, Options);
        Assert.Equal(HttpStatusCode.OK, water.StatusCode);
        var after = await water.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.Null(after!.SoilWetUntil);
        Assert.Equal(PlantDueStatus.Upcoming, after.DueStatus);
    }

    [Fact]
    public async Task SoilWet_ExtendsFromExistingWhenStillActive()
    {
        var plant = await CreateOverduePlant("Keep topping up");
        await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/soil-wet", new { days = 2 }, Options);
        var response = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/soil-wet", new { days = 3 }, Options);
        var deferred = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.True(deferred!.SoilWetUntil > DateTime.UtcNow.AddDays(4));
    }

    [Fact]
    public async Task SoilWet_RejectsOutOfRangeDays()
    {
        var plant = await CreateOverduePlant("Boundary");

        var tooFew = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/soil-wet", new { days = 0 }, Options);
        Assert.Equal(HttpStatusCode.BadRequest, tooFew.StatusCode);

        var tooMany = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/soil-wet", new { days = 31 }, Options);
        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
    }

    [Fact]
    public async Task SoilWet_UnknownPlant_Returns404()
    {
        var set = await _client.PostAsJsonAsync("/api/plants/424242/soil-wet", new { days = 3 }, Options);
        Assert.Equal(HttpStatusCode.NotFound, set.StatusCode);

        var clear = await _client.DeleteAsync("/api/plants/424242/soil-wet");
        Assert.Equal(HttpStatusCode.NotFound, clear.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
