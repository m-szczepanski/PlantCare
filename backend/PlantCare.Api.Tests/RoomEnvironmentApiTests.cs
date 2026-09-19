using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using Xunit;

namespace PlantCare.Api.Tests;

public class RoomEnvironmentApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public RoomEnvironmentApiTests()
    {
        var factory = _database.CreateFactory();
        _client = factory.CreateClient();
    }

    private async Task<int> CreateProfile(string commonName, LightRequirement light)
    {
        var response = await _client.PostAsJsonAsync("/api/plant-profiles", new PlantProfileRequestDto
        {
            CommonName = commonName,
            ScientificName = null,
            DefaultWateringIntervalDays = 7,
            LightRequirement = light,
            HumidityNotes = "Average.",
            CareTips = "Easy going.",
        }, Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlantProfileResponseDto>(Options))!.Id;
    }

    private async Task<PlantResponseDto> CreatePlant(string nickName, int? roomId, int? profileId)
    {
        var response = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            roomId,
            plantProfileId = profileId,
        }, Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    [Fact]
    public async Task RoomStoresEnvironmentParameters()
    {
        var response = await _client.PostAsJsonAsync("/api/rooms", new
        {
            name = "Greenhouse corner",
            orientation = "South",
            lightExposure = "DirectSun",
            humidity = "High",
            temperatureCelsius = 24,
        }, Options);
        response.EnsureSuccessStatusCode();

        var room = await response.Content.ReadFromJsonAsync<RoomResponseDto>(Options);

        Assert.Equal(LightRequirement.DirectSun, room!.LightExposure);
        Assert.Equal(HumidityLevel.High, room.Humidity);
        Assert.Equal(24, room.TemperatureCelsius);
    }

    [Fact]
    public async Task RoomTemperatureOutOfRange_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/rooms", new
        {
            name = "Sauna",
            temperatureCelsius = 80,
        }, Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlantLightMatch_ComputedAgainstRoomExposure()
    {
        var profileId = await CreateProfile("Matcho " + Guid.NewGuid().ToString("N")[..8], LightRequirement.Bright);

        var darkRoom = (await _client.PostAsJsonAsync("/api/rooms", new { name = "Dark " + Guid.NewGuid().ToString("N")[..8], lightExposure = "Low" }, Options)).Content;
        var dark = await darkRoom.ReadFromJsonAsync<RoomResponseDto>(Options);
        var brightRoom = (await _client.PostAsJsonAsync("/api/rooms", new { name = "Bright " + Guid.NewGuid().ToString("N")[..8], lightExposure = "Bright" }, Options)).Content;
        var bright = await brightRoom.ReadFromJsonAsync<RoomResponseDto>(Options);

        var struggling = await CreatePlant("Struggling Steve", dark!.Id, profileId);
        var happy = await CreatePlant("Happy Hedy", bright!.Id, profileId);
        var unknown = await CreatePlant("Unknown Ursula", dark.Id, null);

        Assert.Equal(RoomLightMatch.MuchTooDark, struggling.RoomLightMatch);
        Assert.Equal(RoomLightMatch.Good, happy.RoomLightMatch);
        Assert.Null(unknown.RoomLightMatch);
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}
