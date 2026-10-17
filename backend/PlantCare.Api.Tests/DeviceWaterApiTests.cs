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

public sealed class FakeDeviceBus : IDeviceBus
{
    public event Func<string, string, Task>? MessageReceived;

    public bool IsConnected => true;

    public List<(string Topic, string Payload)> Published { get; } = [];

    public Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        Published.Add((topic, payload));
        return Task.CompletedTask;
    }

    public async Task RaiseAsync(string topic, string payload)
    {
        var handler = MessageReceived;
        if (handler is not null)
        {
            await handler(topic, payload);
        }
    }
}

public class DeviceWaterApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly FakeDeviceBus _bus = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DeviceWaterApiTests()
    {
        _factory = _database.CreateFactory(
            configureBuilder: builder => builder
                .UseSetting("ENABLE_DEVICES", "true")
                .UseSetting("MQTT_HOST", "127.0.0.1")
                .UseSetting("MQTT_PORT", "1"),
            configureTestServices: services => services.AddSingleton<IDeviceBus>(_bus));
        _client = _factory.CreateClient();
    }

    private async Task<DeviceResponseDto> CreateDevice(
        DeviceType type = DeviceType.Valve, CommandStyle style = CommandStyle.Pulse)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/devices",
            new { name = "Pump", type, commandStyle = style }, Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DeviceResponseDto>(Options))!;
    }

    private async Task<PlantResponseDto> CreatePlant(int deviceId)
    {
        var response = await _client.PostAsJsonAsync("/api/plants", new { nickName = "Fern", deviceId }, Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    [Fact]
    public async Task DeviceWater_PublishesPulseCommand()
    {
        var device = await CreateDevice();
        var plant = await CreatePlant(device.Id);

        var response = await _client.PostAsJsonAsync(
            $"/api/plants/{plant.Id}/device-water",
            new { durationMs = 20000 }, Options);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var (topic, payload) = Assert.Single(_bus.Published);
        Assert.Equal($"plantcare/{device.Id}/command", topic);
        Assert.Equal($"{{\"action\":\"water\",\"durationMs\":20000,\"plantId\":{plant.Id}}}", payload);
    }

    [Fact]
    public async Task DeviceWater_OnOffStyle_PublishesOn()
    {
        var device = await CreateDevice(style: CommandStyle.OnOff);
        var plant = await CreatePlant(device.Id);

        await _client.PostAsync($"/api/plants/{plant.Id}/device-water", null);

        var (_, payload) = Assert.Single(_bus.Published);
        Assert.Equal("ON", payload);
    }

    [Fact]
    public async Task DeviceWater_NoDevice_Returns400()
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new { nickName = "NoDevice" }, Options);
        var plant = (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;

        var response = await _client.PostAsync($"/api/plants/{plant.Id}/device-water", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WateredFeedback_MarksWatered_AndDedups()
    {
        var device = await CreateDevice();
        var plant = await CreatePlant(device.Id);

        await _bus.RaiseAsync($"plantcare/{device.Id}/state", "{\"watered\":true,\"moisturePct\":70}");

        var fetched = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plant.Id}", Options);
        Assert.NotNull(fetched!.LastWateredAt);

        var logs = await _client.GetFromJsonAsync<List<WateringLogResponseDto>>(
        $"/api/plants/{plant.Id}/watering-logs", Options);
        Assert.Single(logs!);

        // A duplicate watered:true inside the dedup window must not add a second log.
        await _bus.RaiseAsync($"plantcare/{device.Id}/state", "{\"watered\":true,\"moisturePct\":70}");
        var logsAfter = await _client.GetFromJsonAsync<List<WateringLogResponseDto>>(
        $"/api/plants/{plant.Id}/watering-logs", Options);
        Assert.Single(logsAfter!);
    }

    [Fact]
    public async Task MoistureReading_WetThenDry_DefersThenClears()
    {
        var device = await CreateDevice(type: DeviceType.Sensor);
        var plant = await CreatePlant(device.Id);

        await _bus.RaiseAsync($"plantcare/{device.Id}/state", "{\"moisturePct\":70}");
        var wet = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plant.Id}", Options);
        Assert.NotNull(wet!.SoilWetUntil);
        Assert.Equal(70, wet.SoilMoisturePct);

        await _bus.RaiseAsync($"plantcare/{device.Id}/state", "{\"moisturePct\":30}");
        var dry = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plant.Id}", Options);
        Assert.Null(dry!.SoilWetUntil);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
