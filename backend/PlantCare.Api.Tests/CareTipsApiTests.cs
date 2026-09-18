using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using Xunit;

namespace PlantCare.Api.Tests;

public class CareTipsApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public CareTipsApiTests()
    {
        _client = _database.CreateFactory().CreateClient();
    }

    private async Task<(int ProfileId, int PlantId)> CreateProfiledPlantAsync()
    {
        var profileResponse = await _client.PostAsJsonAsync("/api/plant-profiles", new
        {
            commonName = "Monstera Test",
            defaultWateringIntervalDays = 7,
            lightRequirement = "Bright",
            humidityNotes = "Loves misting.",
            careTips = "Wipe the leaves now and then.",
        }, Options);
        var profile = await profileResponse.Content.ReadFromJsonAsync<PlantProfileResponseDto>(Options);
        Assert.NotNull(profile);

        var plantResponse = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Test Terry",
            location = "Corner",
            plantProfileId = profile.Id,
        }, Options);
        var plant = await plantResponse.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(plant);

        return (profile.Id, plant.Id);
    }

    [Fact]
    public async Task Detail_PlantWithProfile_ExposesCareTips()
    {
        var (_, plantId) = await CreateProfiledPlantAsync();

        var detail = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plantId}", Options);

        Assert.NotNull(detail?.CareTips);
        Assert.Equal("Monstera Test", detail.CareTips.CommonName);
        Assert.Equal("Bright", detail.CareTips.LightRequirement);
        Assert.Equal("Loves misting.", detail.CareTips.HumidityNotes);
        Assert.Equal("Wipe the leaves now and then.", detail.CareTips.CareTips);
    }

    [Fact]
    public async Task Detail_PlantWithoutProfile_HasNoCareTips()
    {
        var response = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Lone Larry",
            location = "Desk",
            customWateringIntervalDays = 7,
        }, Options);
        var plant = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(plant);

        var detail = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plant.Id}", Options);

        Assert.Null(detail!.CareTips);
    }

    [Fact]
    public async Task CareTipsFlagOff_DetailOmitsTipsWithoutCodeChange()
    {
        var (_, plantId) = await CreateProfiledPlantAsync();

        await using var flagOffFactory = _database.CreateFactory(
            configureBuilder: b => b.UseSetting("ENABLE_CARE_TIPS", "false"));
        var http = flagOffFactory.CreateClient();

        var detail = await http.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plantId}", Options);

        Assert.NotNull(detail);
        Assert.Equal("Monstera Test", detail.ProfileCommonName);
        Assert.Null(detail.CareTips);
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}
