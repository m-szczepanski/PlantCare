using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class NotificationApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private sealed class RecordingPublisher : INtfyPublisher
    {
        public (string Title, string Message, int Priority)? Last { get; private set; }

        public int Count { get; private set; }

        public Task PublishAsync(string title, string message, int priority = 3, string? clickUrl = null, string? buttonLabel = null, string? buttonUrl = null, CancellationToken cancellationToken = default)
        {
            Last = (title, message, priority);
            Count += 1;
            return Task.CompletedTask;
        }
    }

    private sealed class BrokenChannel : INotificationChannel
    {
        public string Name => "broken";

        public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
            => throw new HttpRequestException("channel down");
    }

    private readonly TempDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private HttpClient ClientWith(Action<IServiceCollection>? extra = null)
    {
        var publisher = new RecordingPublisher();
        var factory = _database.CreateFactory(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(publisher);
            extra?.Invoke(services);
        });
        return factory.CreateClient();
    }

    [Fact]
    public async Task TestNotification_DeliversLocalizedDefaultThroughNtfy()
    {
        var client = ClientWith();

        var response = await client.PostAsJsonAsync("/api/notifications/test", new { message = (string?)null }, Options);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<NotificationTestResult>(Options);
        Assert.NotNull(result);
        Assert.True(result!.AnyDelivered);
        Assert.Contains(result.Channels, c => c.Name == "ntfy" && c.Delivered);
    }

    [Fact]
    public async Task TestNotification_CustomMessagePassesThrough()
    {
        var publisher = new RecordingPublisher();
        var factory = _database.CreateFactory(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(publisher);
        });
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/notifications/test", new { message = "ping from the status page" }, Options);

        Assert.Equal("ping from the status page", publisher.Last?.Message);
    }

    [Fact]
    public async Task TestNotification_ReportsFailurePerChannelWithoutThrowing()
    {
        var client = ClientWith(services => services.AddSingleton<INotificationChannel>(new BrokenChannel()));

        var response = await client.PostAsJsonAsync("/api/notifications/test", new { }, Options);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<NotificationTestResult>(Options);
        Assert.NotNull(result);
        Assert.True(result!.AnyDelivered);
        Assert.Equal(2, result.Channels.Count);
        var broken = result.Channels.Single(c => c.Name == "broken");
        Assert.False(broken.Delivered);
        Assert.NotNull(broken.Error);
    }

    [Fact]
    public async Task RunCheck_PublishesDigestForDuePlant()
    {
        var publisher = new RecordingPublisher();
        var factory = _database.CreateFactory(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(publisher);
        });
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Manual Terry",
            customWateringIntervalDays = 7,
            lastWateredAt = DateTime.UtcNow.Date.AddDays(-10),
        });

        var response = await client.PostAsync("/api/notifications/run-check", null);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<WateringCheckResult>(Options);
        Assert.Equal(1, result!.SentDigests);
        Assert.Equal(1, publisher.Count);
    }

    [Fact]
    public async Task Status_ExposesConfiguredChannels()
    {
        var client = _database.CreateFactory().CreateClient();

        var status = await client.GetFromJsonAsync<StatusInfo>("/api/status", Options);

        Assert.NotNull(status);
        Assert.Contains("ntfy", status!.Channels);
    }
}
