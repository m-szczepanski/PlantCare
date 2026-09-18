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
        public List<(string Title, string Message)> Published { get; } = [];

        public Func<(string Title, string Message), bool>? FailWhen { get; set; }

        public Task PublishAsync(string title, string message, CancellationToken cancellationToken = default)
        {
            if (FailWhen is not null && FailWhen((title, message)))
            {
                throw new HttpRequestException("ntfy unreachable");
            }

            Published.Add((title, message));
            return Task.CompletedTask;
        }
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

    private async Task<PlantResponseDto> CreatePlant(string nickName, int? intervalDays, int? daysSinceWatered)
        => await CreatePlant(nickName, intervalDays, daysSinceWatered is null ? null : Today.AddDays(-daysSinceWatered.Value));

    private async Task<PlantResponseDto> CreatePlant(string nickName, int? intervalDays, DateTime? lastWateredAt)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            location = "Desk",
            customWateringIntervalDays = intervalDays,
            lastWateredAt,
        }, Options);

        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    [Fact]
    public async Task Run_OnlyOverdueAndDueTodayPlants_ArePublished()
    {
        var overdue = await CreatePlant("Thirsty", 7, 30);
        var dueToday = await CreatePlant("Parched", 7, 7);
        var watered = await CreatePlant("Fresh", 7, 0);
        var unscheduled = await CreatePlant("Wildcard", null, (int?)null);

        var result = await RunCheckAsync();

        Assert.Equal(2, result.Sent);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.SkippedDuplicates);

        Assert.Contains(_publisher.Published, m => m.Message.StartsWith(overdue.NickName));
        Assert.Contains(_publisher.Published, m => m.Message.StartsWith(dueToday.NickName));
        Assert.DoesNotContain(_publisher.Published, m => m.Message.StartsWith(watered.NickName));
        Assert.DoesNotContain(_publisher.Published, m => m.Message.StartsWith(unscheduled.NickName));
        Assert.All(_publisher.Published, m => Assert.Equal("Watering due", m.Title));
    }

    [Fact]
    public async Task Run_TwiceSameDay_SecondRunSendsNothing()
    {
        var overdue = await CreatePlant("Thirsty", 7, 30);

        await RunCheckAsync();
        var second = await RunCheckAsync();

        Assert.Equal(0, second.Sent);
        Assert.Equal(1, second.SkippedDuplicates);
        Assert.Single(_publisher.Published);

        var logs = await GetNotificationLogsAsync();
        Assert.Equal([overdue.Id], logs.Select(l => l.PlantId));
        Assert.All(logs, l => Assert.Equal(NotificationType.WateringDue, l.Type));
    }

    [Fact]
    public async Task Run_PublisherFailsForOnePlant_OthersStillSentAndFailureNotLogged()
    {
        await CreatePlant("Fails", 7, 30);
        var ok = await CreatePlant("Succeeds", 7, 30);

        _publisher.FailWhen = m => m.Message.StartsWith("Fails");
        var result = await RunCheckAsync();

        Assert.Equal(1, result.Sent);
        Assert.Equal(1, result.Failed);
        Assert.Contains(_publisher.Published, m => m.Message.StartsWith(ok.NickName));

        var logs = await GetNotificationLogsAsync();
        Assert.Equal([ok.Id], logs.Select(l => l.PlantId));
    }

    [Fact]
    public async Task Run_AfterWatering_PlantIsNoLongerDue()
    {
        var overdue = await CreatePlant("Thirsty", 7, 30);
        await _client.PostAsJsonAsync($"/api/plants/{overdue.Id}/water", new { });

        var result = await RunCheckAsync();

        Assert.Equal(0, result.Sent);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task Run_OldNotificationLog_DoesNotSuppressToday()
    {
        var overdue = await CreatePlant("Thirsty", 7, 30);
        await using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.NotificationLogs.Add(new NotificationLog
            {
                PlantId = overdue.Id,
                Type = NotificationType.WateringDue,
                SentAt = Today.AddDays(-1).AddHours(12),
            });
            await db.SaveChangesAsync();
        }

        var result = await RunCheckAsync();

        Assert.Equal(1, result.Sent);
        Assert.Equal(0, result.SkippedDuplicates);
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

    private async Task<List<NotificationLog>> GetNotificationLogsAsync()
    {
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.NotificationLogs.OrderBy(n => n.Id).ToListAsync();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
