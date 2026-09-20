using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class StatusApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private sealed class SilentPublisher : INtfyPublisher
    {
        public Task PublishAsync(string title, string message, int priority = 3, string? clickUrl = null, string? buttonLabel = null, string? buttonUrl = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public StatusApiTests()
    {
        _client = _database.CreateFactory().CreateClient();
    }

    [Fact]
    public async Task Status_EmptyInstance_ReportsUnreachableNtfyAndNoRuns()
    {
        var status = await _client.GetFromJsonAsync<StatusInfo>("/api/status", Options);

        Assert.NotNull(status);
        Assert.Null(status!.LastJobRun);
        Assert.Null(status.LastDigest);
        Assert.Equal("0 8 * * *", status.WateringCheckCron);
        Assert.False(status.Ntfy.Reachable);
        Assert.Contains("/plant-care", status.Ntfy.SubscribeUrl);
    }

    [Fact]
    public async Task Status_RecordsLastCheckRunAndDigest()
    {
        await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Status Sam",
            customWateringIntervalDays = 7,
            lastWateredAt = DateTime.UtcNow.Date.AddDays(-10),
        }, Options);

        var factory = _database.CreateFactory(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
            services.AddSingleton<INtfyPublisher>(new SilentPublisher());
        });
        var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var check = scope.ServiceProvider.GetRequiredService<IWateringCheckService>();
            await check.RunAsync();
        }

        var status = await client.GetFromJsonAsync<StatusInfo>("/api/status", Options);

        Assert.Equal("digest-sent", status!.LastJobRun!.Outcome);
        Assert.Equal(1, status.LastDigest!.PlantCount);
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}
