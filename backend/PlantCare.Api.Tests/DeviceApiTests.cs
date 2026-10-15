using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using Xunit;

namespace PlantCare.Api.Tests;

public class DeviceApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DeviceApiTests()
    {
        _factory = _database.CreateFactory(configureBuilder: builder => builder.UseSetting("ENABLE_DEVICES", "true"));
        _client = _factory.CreateClient();
    }

    private async Task<DeviceResponseDto> CreateDevice(string name, DeviceType type = DeviceType.Valve)
    {
        var response = await _client.PostAsJsonAsync("/api/devices", new { name, type }, Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var device = await response.Content.ReadFromJsonAsync<DeviceResponseDto>(Options);
        Assert.NotNull(device);
        return device;
    }

    [Fact]
    public async Task CreateDevice_ReturnsDevice_WithDefaults()
    {
        var device = await CreateDevice("Pump 1", DeviceType.Pump);

        Assert.Equal("Pump 1", device.Name);
        Assert.Equal(DeviceType.Pump, device.Type);
        Assert.Equal(CommandStyle.Pulse, device.CommandStyle);
        Assert.Equal(0, device.PlantCount);
        Assert.Null(device.LastMoisturePct);
    }

    [Fact]
    public async Task UpdateDevice_ChangesFields()
    {
        var device = await CreateDevice("Valve B");

        var update = await _client.PutAsJsonAsync($"/api/devices/{device.Id}", new
        {
            name = "Valve C",
            type = DeviceType.Sensor,
            commandStyle = CommandStyle.OnOff,
            commandTopic = "zigbee2mqtt/valve/set",
        }, Options);

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<DeviceResponseDto>(Options);
        Assert.Equal("Valve C", updated!.Name);
        Assert.Equal(DeviceType.Sensor, updated.Type);
        Assert.Equal(CommandStyle.OnOff, updated.CommandStyle);
        Assert.Equal("zigbee2mqtt/valve/set", updated.CommandTopic);
    }

    [Fact]
    public async Task UpdateDevice_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/devices/424242", new { name = "Nope" }, Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDevice_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/devices/424242");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LinkDeviceToPlant_SurfacesDeviceId_WithNullReading()
    {
        var device = await CreateDevice("Pump linked");

        var created = await _client.PostAsJsonAsync("/api/plants", new { nickName = "Fern", deviceId = device.Id }, Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var plant = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.Equal(device.Id, plant!.DeviceId);
        Assert.Null(plant.SoilMoisturePct);
        Assert.Null(plant.SoilMoistureAgeMinutes);
    }

    [Fact]
    public async Task PlantResponse_SurfacesLatestReading_WhenDeviceReports()
    {
        var device = await CreateDevice("Moisture sensor", DeviceType.Sensor);
        await _client.PostAsJsonAsync("/api/plants", new { nickName = "Calathea", deviceId = device.Id }, Options);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tracked = await db.Devices.SingleAsync(d => d.Id == device.Id);
            tracked.LastMoisturePct = 42;
            tracked.WaterLevel = DeviceWaterLevel.Ok;
            tracked.LastSeenAt = DateTime.UtcNow.AddMinutes(-5);
            await db.SaveChangesAsync();
        }

        var plants = await _client.GetFromJsonAsync<List<PlantResponseDto>>("/api/plants", Options);
        var plant = plants!.Single(p => p.NickName == "Calathea");

        Assert.Equal(42, plant.SoilMoisturePct);
        Assert.NotNull(plant.SoilMoistureAgeMinutes);
        Assert.InRange(plant.SoilMoistureAgeMinutes!.Value, 4, 6);
    }

    [Fact]
    public async Task DeleteDevice_SetsPlantDeviceIdNull()
    {
        var device = await CreateDevice("Pump to delete");
        var created = await _client.PostAsJsonAsync("/api/plants", new { nickName = "Orchid", deviceId = device.Id }, Options);
        var plant = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        var delete = await _client.DeleteAsync($"/api/devices/{device.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var fetched = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plant!.Id}", Options);
        Assert.Null(fetched!.DeviceId);
        Assert.Null(fetched.SoilMoisturePct);
    }

    [Fact]
    public async Task PlantWithUnknownDevice_Returns400()
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new { nickName = "No device", deviceId = 424242 }, Options);

        Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
    }

    [Fact]
    public async Task UpdatePlant_CanUnlinkDevice()
    {
        var device = await CreateDevice("Valve unlink");
        var created = await _client.PostAsJsonAsync("/api/plants", new { nickName = "Ivy", deviceId = device.Id }, Options);
        var plant = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        var update = await _client.PutAsJsonAsync($"/api/plants/{plant!.Id}", new { nickName = "Ivy", deviceId = (int?)null }, Options);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.Null(updated!.DeviceId);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}

public class DeviceDisabledApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public DeviceDisabledApiTests()
    {
        _client = _database.CreateFactory().CreateClient();
    }

    [Fact]
    public async Task DevicesEndpoints_Return404_WhenDisabled()
    {
        var response = await _client.GetAsync("/api/devices");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PlantCreate_IgnoresDeviceId_WhenDisabled()
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new { nickName = "No hardware", deviceId = 1 }, Options);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var plant = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.Null(plant!.DeviceId);
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}
