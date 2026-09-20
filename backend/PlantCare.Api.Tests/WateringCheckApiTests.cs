using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class WateringCheckApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private sealed class RecordingPublisher : INtfyPublisher
    {
        public List<(string Title, string Message, int Priority)> Published { get; } = [];

        public bool FailNext { get; set; }

        public Task PublishAsync(string title, string message, int priority = 3, string? clickUrl = null, string? buttonLabel = null, string? buttonUrl = null, CancellationToken cancellationToken = default)
        {
            if (FailNext)
            {
                FailNext = false;
                throw new HttpRequestException("ntfy unreachable");
            }

            Published.Add((title, message, priority));
            LastButtonUrl = buttonUrl;
            LastClickUrl = clickUrl;
            return Task.CompletedTask;
        }

        public string? LastButtonUrl { get; private set; }
        public string? LastClickUrl { get; private set; }
    }

    private static DateTime Today => DateTime.UtcNow.Date;

    private readonly RecordingPublisher _publisher = new();
    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WateringCheckApiTests()
    {
        _factory = _database.CreateFactory(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(_publisher);
        });
        _client = _factory.CreateClient();
    }

    private AsyncServiceScope CreateScope() => _factory.Services.CreateAsyncScope();

    private async Task<WateringCheckResult> RunCheckAsync()
    {
        await using var scope = CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IWateringCheckService>().RunAsync();
    }

    private async Task<PlantResponseDto> CreatePlant(string nickName, int? intervalDays, int? daysSinceWatered, bool notifyEnabled = true)
        => await CreatePlant(nickName, intervalDays, daysSinceWatered is null ? null : Today.AddDays(-daysSinceWatered.Value), notifyEnabled);

    private async Task<PlantResponseDto> CreatePlant(string nickName, int? intervalDays, DateTime? lastWateredAt, bool notifyEnabled = true)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            customWateringIntervalDays = intervalDays,
            lastWateredAt,
            notifyEnabled,
        }, Options);

        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    [Fact]
    public async Task Run_SendsOneDigestForDuePlants_AtDefaultPriority()
    {
        await CreatePlant("Due one", 7, 7);
        await CreatePlant("Due two", 7, 7);
        await CreatePlant("Fresh", 7, 0);
        await CreatePlant("Wildcard", null, (int?)null);

        var result = await RunCheckAsync();

        Assert.Equal(1, result.SentDigests);
        var digest = Assert.Single(_publisher.Published);
        Assert.Equal("2 plants need water", digest.Title);
        Assert.Contains("• Due one", digest.Message);
        Assert.Contains("• Due two", digest.Message);
        Assert.DoesNotContain("Fresh", digest.Message);
        Assert.DoesNotContain("Wildcard", digest.Message);
        Assert.Equal(3, digest.Priority);
    }

    [Fact]
    public async Task Run_WithOverdue_EscalatesPriorityAndTitle()
    {
        await CreatePlant("Thirsty", 7, 30);
        await CreatePlant("Due", 7, 7);

        await RunCheckAsync();

        var digest = Assert.Single(_publisher.Published);
        Assert.Equal("2 plants need water (1 overdue)", digest.Title);
        Assert.Equal(5, digest.Priority);
    }

    [Fact]
    public async Task Run_SkipsMutedPlants()
    {
        await CreatePlant("Quiet Carol", 7, 30, notifyEnabled: false);
        await CreatePlant("Loud Larry", 7, 30);

        var result = await RunCheckAsync();

        Assert.Equal(1, result.PlantsInDigest);
        var digest = Assert.Single(_publisher.Published);
        Assert.DoesNotContain("Quiet Carol", digest.Message);
    }

    [Fact]
    public async Task Run_TwiceSameDay_SecondRunSendsNothing()
    {
        await CreatePlant("Thirsty", 7, 30);

        await RunCheckAsync();
        var second = await RunCheckAsync();

        Assert.Equal(0, second.SentDigests);
        Assert.Equal(1, second.SkippedDuplicates);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task Run_PublisherFails_NoDigestLogged_SameDayRetrySucceeds()
    {
        await CreatePlant("Thirsty", 7, 30);
        _publisher.FailNext = true;

        var failed = await RunCheckAsync();

        Assert.Equal(1, failed.Failed);
        Assert.Equal(0, failed.SentDigests);

        var retried = await RunCheckAsync();

        Assert.Equal(1, retried.SentDigests);
        Assert.Equal(1, _publisher.Published.Count);
    }

    [Fact]
    public async Task Run_AfterWatering_PlantIsNoLongerDue()
    {
        var overdue = await CreatePlant("Thirsty", 7, 30);
        await _client.PostAsJsonAsync($"/api/plants/{overdue.Id}/water", new { });

        var result = await RunCheckAsync();

        Assert.Equal(0, result.SentDigests);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task Run_ToxicPlantDigest_AnnotatesToxicity()
    {
        var profile = await _client.PostAsJsonAsync("/api/plant-profiles", new
        {
            commonName = "Toxic Tina Fern",
            defaultWateringIntervalDays = 5,
            lightRequirement = "Medium",
            humidityNotes = "x",
            careTips = "y",
            toxicToPets = true,
            toxicToChildren = false,
        }, Options);
        profile.EnsureSuccessStatusCode();
        var profileId = (await profile.Content.ReadFromJsonAsync<PlantProfileResponseDto>(Options))!.Id;

        await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Tina",
            customWateringIntervalDays = 5,
            lastWateredAt = Today.AddDays(-12),
            plantProfileId = profileId,
        }, Options);

        await RunCheckAsync();

        var digest = Assert.Single(_publisher.Published);
        Assert.Contains("[toxic to pets]", digest.Message);
    }

    [Fact]
    public async Task Run_SingleDuePlantWithQuickActions_EmitsWaterButton()
    {
        using var withSecret = _database.CreateFactory(configureBuilder: builder =>
        {
            builder.UseSetting("QUICK_ACTION_SECRET", "s3cret");
            builder.UseSetting("QUICK_ACTION_URL_BASE", "http://plant.lan:3000/");
        }, configureTestServices: services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(_publisher);
        });

        var client = withSecret.CreateClient();
        var created = await client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Only one",
            customWateringIntervalDays = 7,
            lastWateredAt = Today.AddDays(-10),
        }, Options);
        var plant = (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;

        await using var scope = withSecret.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IWateringCheckService>().RunAsync();

        Assert.Equal($"http://plant.lan:3000/api/plants/{plant.Id}/quick-water?key=s3cret", _publisher.LastButtonUrl);
        Assert.Equal("http://plant.lan:3000", _publisher.LastClickUrl);
    }

    [Fact]
    public async Task DigestRecorded_PlantAndOverdueCountsPersisted()
    {
        await CreatePlant("Thirsty", 7, 30);

        await RunCheckAsync();

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var digest = Assert.Single(await db.NotificationDigests.ToListAsync());
        Assert.Equal(1, digest.PlantCount);
        Assert.Equal(1, digest.OverdueCount);
        Assert.Equal(5, digest.Priority);
    }

    [Fact]
    public async Task ScheduledJob_IsResolvableFromRootProvider_CoravelActivatesItPerRun()
    {
        // Coravel resolves the IInvocable via GetRequiredService in a fresh scope; if the
        // registration is missing the job silently never runs, so assert it resolves.
        await using var scope = _factory.Services.CreateAsyncScope();

        var job = scope.ServiceProvider.GetRequiredService<WateringCheckJob>();

        Assert.NotNull(job);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
