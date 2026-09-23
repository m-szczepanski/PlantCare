using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class HealthCheckReminderApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private sealed class RecordingPublisher : INtfyPublisher
    {
        public List<(string Title, string Message)> Published { get; } = [];

        public Task PublishAsync(string title, string message, int priority = 3, string? clickUrl = null, string? buttonLabel = null, string? buttonUrl = null, CancellationToken cancellationToken = default)
        {
            Published.Add((title, message));
            return Task.CompletedTask;
        }
    }

    private static DateTime Today => DateTime.UtcNow.Date;

    private readonly RecordingPublisher _publisher = new();
    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckReminderApiTests()
    {
        _factory = _database.CreateFactory(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(_publisher);
        });
        _client = _factory.CreateClient();
    }

    private async Task<PlantResponseDto> CreatePlant(string nickName, int acquiredDaysAgo = 60, int? daysSinceWatered = null, bool notifyEnabled = true)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            customWateringIntervalDays = 7,
            lastWateredAt = daysSinceWatered is null ? Today : Today.AddDays(-daysSinceWatered.Value),
            acquiredDate = Today.AddDays(-acquiredDaysAgo),
            notifyEnabled,
        }, Options);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    private async Task<WateringCheckResult> RunCheckAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IWateringCheckService>().RunAsync();
    }

    private async Task ClearDigestHistoryAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.NotificationDigests.RemoveRange(await db.NotificationDigests.ToListAsync());
        await db.SaveChangesAsync();
    }

    private async Task SetReminderStampAsync(int plantId, DateTime? stamp)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plant = await db.Plants.FirstAsync(p => p.Id == plantId);
        plant.CheckupReminderSentAt = stamp;
        await db.SaveChangesAsync();
    }

    private async Task<DateTime?> GetReminderStampAsync(int plantId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.Plants.FirstAsync(p => p.Id == plantId)).CheckupReminderSentAt;
    }

    [Fact]
    public async Task CheckupsOnly_SendsItsOwnDigest_AndStampsReminders()
    {
        var thirsty = await CreatePlant("Sipped Sam", daysSinceWatered: 1);

        var result = await RunCheckAsync();

        Assert.Equal(1, result.SentDigests);
        var digest = Assert.Single(_publisher.Published);
        Assert.Equal("1 health checkup is due", digest.Title);
        Assert.Contains("Health checkups due (1):", digest.Message);
        Assert.Contains("• Sipped Sam", digest.Message);
        Assert.NotNull(await GetReminderStampAsync(thirsty.Id));
    }

    [Fact]
    public async Task CheckupReminder_NotResentWithinThePeriod()
    {
        await CreatePlant("Nagged Nancy");
        await RunCheckAsync();

        // Fresh digest day (bypass the once-per-day cap); the plant-level stamp
        // must still hold the reminder back.
        await ClearDigestHistoryAsync();
        await RunCheckAsync();

        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task CheckupReminder_ResentAfterThirtyDays()
    {
        var plant = await CreatePlant("Forgetful Fred");
        await SetReminderStampAsync(plant.Id, DateTime.UtcNow.AddDays(-31));

        await RunCheckAsync();

        var digest = Assert.Single(_publisher.Published);
        Assert.Contains("Forgetful Fred", digest.Message);
    }

    [Fact]
    public async Task CombinedDigest_KeepsWateringTitle_AndAddsCheckupSection()
    {
        var paula = await CreatePlant("Parched Paula", daysSinceWatered: 7);
        await CreatePlant("Checked Chuck", acquiredDaysAgo: 60, daysSinceWatered: 1);
        // Paula already got a checkup nudge recently; only her watering is due now.
        await SetReminderStampAsync(paula.Id, DateTime.UtcNow.AddDays(-1));

        var result = await RunCheckAsync();

        Assert.Equal(1, result.SentDigests);
        var digest = Assert.Single(_publisher.Published);
        Assert.Equal("1 plant needs water", digest.Title);
        Assert.Contains("Health checkups due (1):", digest.Message);
        Assert.Contains("• Checked Chuck", digest.Message);
        // Chuck is the sole stale checkup; Paula is reminded-free but water-thirsty.
        var checkupSection = digest.Message.Split("Health checkups due")[1];
        Assert.DoesNotContain("Parched Paula", checkupSection);
    }

    [Fact]
    public async Task CheckupSection_SkipsMutedAndSnoozedPlants()
    {
        await CreatePlant("Mute Marge", notifyEnabled: false);
        var snoozed = await CreatePlant("Holiday Hal");
        await _client.PostAsJsonAsync($"/api/plants/{snoozed.Id}/snooze", new { days = 14 }, Options);

        var result = await RunCheckAsync();

        Assert.Equal(0, result.SentDigests);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task FreshPlant_NoCheckupSection()
    {
        await CreatePlant("Brand New Nora", acquiredDaysAgo: 3, daysSinceWatered: 7);

        await RunCheckAsync();

        var digest = Assert.Single(_publisher.Published);
        Assert.Equal("1 plant needs water", digest.Title);
        Assert.DoesNotContain("checkup", digest.Message);
    }

    [Fact]
    public async Task AnsweringCheckup_StopsTheReminder()
    {
        var plant = await CreatePlant("Answered Alice");
        await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/health-checks", new { status = "Good" }, Options);

        var result = await RunCheckAsync();

        Assert.Equal(0, result.SentDigests);
        Assert.Empty(_publisher.Published);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
