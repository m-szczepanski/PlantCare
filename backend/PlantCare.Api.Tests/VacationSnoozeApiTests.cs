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

public class VacationSnoozeApiTests : IDisposable
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

    public VacationSnoozeApiTests()
    {
        _factory = _database.CreateFactory(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(_publisher);
        });
        _client = _factory.CreateClient();
    }

    private async Task<PlantResponseDto> CreateDuePlant(string nickName)
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
    public async Task Snooze_SetsSnoozedUntil_AndCheckSkipsPlant()
    {
        var plant = await CreateDuePlant("On holiday");

        var response = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/snooze", new { days = 14 }, Options);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snoozed = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(snoozed!.SnoozedUntil);
        Assert.True(snoozed.SnoozedUntil > DateTime.UtcNow.AddDays(13));

        var result = await RunCheckAsync();
        Assert.Equal(0, result.SentDigests);
        Assert.Empty(_publisher.Messages);
    }

    [Fact]
    public async Task Snooze_ExtendsFromExistingWhenStillActive()
    {
        var plant = await CreateDuePlant("Serial vacationer");

        await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/snooze", new { days = 3 }, Options);
        var response = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/snooze", new { days = 5 }, Options);
        var snoozed = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.True(snoozed!.SnoozedUntil > DateTime.UtcNow.AddDays(7));
    }

    [Fact]
    public async Task ClearSnooze_RestoresReminders()
    {
        var plant = await CreateDuePlant("Home early");
        await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/snooze", new { days = 10 }, Options);

        var clear = await _client.DeleteAsync($"/api/plants/{plant.Id}/snooze");
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        var restored = await clear.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.Null(restored!.SnoozedUntil);

        var result = await RunCheckAsync();
        Assert.Equal(1, result.SentDigests);
    }

    [Fact]
    public async Task SnoozeAll_CoversEveryPlant()
    {
        var a = await CreateDuePlant("Vacationer A");
        var b = await CreateDuePlant("Vacationer B");

        var response = await _client.PostAsJsonAsync("/api/plants/snooze-all", new { days = 21 }, Options);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SnoozeAllResponseDto>(Options);
        Assert.Equal(2, body!.SnoozedPlants);

        var result = await RunCheckAsync();
        Assert.Equal(0, result.SentDigests);

        await _client.DeleteAsync($"/api/plants/{a.Id}/snooze");
        await _client.DeleteAsync($"/api/plants/{b.Id}/snooze");
        var afterResume = await RunCheckAsync();
        Assert.Equal(1, afterResume.SentDigests);
    }

    [Fact]
    public async Task Snooze_RejectsInvalidDaysAndUnknownPlant()
    {
        var bad = await _client.PostAsJsonAsync("/api/plants/snooze-all", new { days = 0 }, Options);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var plant = await CreateDuePlant("Ghost check");
        var missing = await _client.PostAsJsonAsync("/api/plants/424242/snooze", new { days = 5 }, Options);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
