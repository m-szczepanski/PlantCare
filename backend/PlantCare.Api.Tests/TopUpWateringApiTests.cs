using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class TopUpWateringApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static DateTime Today => DateTime.UtcNow.Date;

    private sealed class RecordingPublisher : INtfyPublisher
    {
        public List<(string Title, string Message)> Published { get; } = [];

        public Task PublishAsync(string title, string message, int priority = 3, string? clickUrl = null, string? buttonLabel = null, string? buttonUrl = null, CancellationToken cancellationToken = default)
        {
            Published.Add((title, message));
            return Task.CompletedTask;
        }
    }

    private readonly RecordingPublisher _publisher = new();
    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TopUpWateringApiTests()
    {
        _factory = _database.CreateFactory(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(_publisher);
        });
        _client = _factory.CreateClient();
    }

    private async Task<PlantResponseDto> CreatePlant(string nickName, SoilType? soilType, int intervalDays = 10, int? wateredDaysAgo = null)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            soilType,
            customWateringIntervalDays = intervalDays,
            lastWateredAt = wateredDaysAgo is null ? (DateTime?)null : Today.AddDays(-wateredDaysAgo.Value),
        }, Options);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    private async Task<PlantResponseDto> GetPlant(int id)
        => (await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{id}", Options))!;

    private async Task<WateringCheckResult> RunCheckAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IWateringCheckService>().RunAsync();
    }

    [Fact]
    public async Task PermeableSoil_PlantCarriesDerivedTopUp()
    {
        // ChunkyBark base cycle 10 -> half is 5; watered (at midnight) 4 days ago
        // -> top-up due tomorrow, watering due in 6 days, unchanged by permeability.
        var plant = await CreatePlant("Bark mono", SoilType.ChunkyBark, intervalDays: 10, wateredDaysAgo: 4);

        Assert.True(plant.TopUpWateringEnabled);
        Assert.Equal(PlantDueStatus.Upcoming, plant.TopUpWateringStatus);
        Assert.Equal(Today.AddDays(1), plant.TopUpWateringNextDueDate!.Value.Date);
        Assert.Equal(1, plant.TopUpWateringDaysUntilDue);
        Assert.NotNull(plant.TopUpWateringMessage);
        Assert.Equal(10, plant.WateringIntervalDays);
        Assert.Equal(Today.AddDays(6), plant.NextDueDate!.Value.Date);
    }

    [Fact]
    public async Task SlowSoil_And_UnsetSoil_HaveNoTopUp()
    {
        var peat = await CreatePlant("Coco", SoilType.PeatCoco);
        Assert.False(peat.TopUpWateringEnabled);
        Assert.Null(peat.TopUpWateringMessage);

        var plain = await CreatePlant("Bare", null);
        Assert.False(plain.TopUpWateringEnabled);
    }

    [Fact]
    public async Task UpdateToPermeableSoil_AddsTopUp_ChangesBack_RemovesIt()
    {
        var plant = await CreatePlant("Switching", SoilType.AllPurpose);
        Assert.False(plant.TopUpWateringEnabled);

        await _client.PutAsJsonAsync($"/api/plants/{plant.Id}", UpdateInput(plant, SoilType.CactusMix), Options);
        var added = await GetPlant(plant.Id);
        Assert.True(added.TopUpWateringEnabled);

        await _client.PutAsJsonAsync($"/api/plants/{plant.Id}", UpdateInput(added, SoilType.SelfWatering), Options);
        var removed = await GetPlant(plant.Id);
        Assert.False(removed.TopUpWateringEnabled);
    }

    private static object UpdateInput(PlantResponseDto plant, SoilType? soilType) => new
    {
        nickName = plant.NickName,
        roomId = plant.RoomId,
        photoUrl = plant.PhotoUrl,
        potSizeCm = plant.PotSizeCm,
        soilType,
        propagatedFrom = plant.PropagatedFrom,
        notifyEnabled = plant.NotifyEnabled,
        acquiredDate = plant.AcquiredDate,
        plantProfileId = plant.PlantProfileId,
        customWateringIntervalDays = plant.CustomWateringIntervalDays,
        lastWateredAt = plant.LastWateredAt,
        reduceInWinter = plant.ReduceInWinter,
    };

    [Fact]
    public async Task CareTasks_ListShowsSingleTopUpRowWithDerivedInterval()
    {
        var plant = await CreatePlant("Listed", SoilType.ChunkyBark, intervalDays: 10, wateredDaysAgo: 4);

        var tasks = await _client.GetFromJsonAsync<CareTaskResponseDto[]>($"/api/plants/{plant.Id}/care-tasks", Options);

        var topUp = Assert.Single(tasks!, t => t.Type == CareTaskType.TopUpWatering);
        Assert.Equal(5, topUp.IntervalDays);
        Assert.Equal(PlantDueStatus.Upcoming, topUp.DueStatus);
        Assert.Contains("small drink", topUp.Hint);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
