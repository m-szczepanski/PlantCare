using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public sealed class RecordingNotificationChannel : INotificationChannel
{
    public string Name => "recording";

    public List<NotificationMessage> Sent { get; } = [];

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}

public class DeviceWateringServiceTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly FakeDeviceBus _bus = new();
    private readonly RecordingNotificationChannel _channel = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DeviceWateringServiceTests()
    {
        _factory = _database.CreateFactory(
            configureBuilder: builder => builder
                .UseSetting("ENABLE_DEVICES", "true")
                .UseSetting("MQTT_HOST", "127.0.0.1")
                .UseSetting("MQTT_PORT", "1")
                .UseSetting("ALLOW_AUTO_WATER_WITHOUT_READING", "true"),
            configureTestServices: services =>
            {
                services.AddSingleton<IDeviceBus>(_bus);
                services.RemoveAll<INotificationChannel>();
                services.AddSingleton<INotificationChannel>(_channel);
            });
        _client = _factory.CreateClient();
    }

    private async Task<DeviceResponseDto> CreateDevice()
    {
        var response = await _client.PostAsJsonAsync("/api/devices", new { name = "Pump" }, Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DeviceResponseDto>(Options))!;
    }

    private async Task<PlantResponseDto> CreateOverduePlant(int deviceId)
    {
        var response = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Fern",
            deviceId,
            acquiredDate = DateTime.UtcNow.AddDays(-30),
            customWateringIntervalDays = 7,
        }, Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    private async Task<DeviceWateringResult> RunWateringAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDeviceWateringService>();
        return await service.RunAsync();
    }

    [Fact]
    public async Task Waters_DueDryPlant_AndLogs()
    {
        var device = await CreateDevice();
        var plant = await CreateOverduePlant(device.Id);

        var result = await RunWateringAsync();

        Assert.Equal(1, result.Watered);
        var (topic, _) = Assert.Single(_bus.Published);
        Assert.Equal($"plantcare/{device.Id}/command", topic);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.DeviceWateringLogs.CountAsync(l => l.PlantId == plant.Id));
    }

    [Fact]
    public async Task PerDayCap_PreventsSecondCommand()
    {
        var device = await CreateDevice();
        await CreateOverduePlant(device.Id);

        await RunWateringAsync();
        await RunWateringAsync();

        Assert.Single(_bus.Published);
    }

    [Fact]
    public async Task SkipsMoistPlant()
    {
        var device = await CreateDevice();
        await CreateOverduePlant(device.Id);

        // 50 is at/above the auto-water threshold (40) but below the wet-deferral
        // threshold (60), so the plant stays due yet is skipped by the guard.
        await _bus.RaiseAsync($"plantcare/{device.Id}/state", "{\"moisturePct\":50}");

        var result = await RunWateringAsync();

        Assert.Equal(0, result.Watered);
        Assert.Empty(_bus.Published);
    }

    [Fact]
    public async Task ReportsFeedbackFailure_Once()
    {
        var device = await CreateDevice();
        var plant = await CreateOverduePlant(device.Id);

        await RunWateringAsync();
        Assert.Single(_bus.Published);
        Assert.Empty(_channel.Sent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var log = await db.DeviceWateringLogs.SingleAsync(l => l.PlantId == plant.Id);
            log.SentAt = DateTime.UtcNow.AddMinutes(-2);
            await db.SaveChangesAsync();
        }

        await RunWateringAsync();
        Assert.Single(_channel.Sent);

        await RunWateringAsync();
        Assert.Single(_channel.Sent);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
