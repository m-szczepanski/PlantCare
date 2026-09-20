using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using Xunit;

namespace PlantCare.Api.Tests;

public class PlantProfileWriteApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public PlantProfileWriteApiTests()
    {
        _client = _database.CreateFactory().CreateClient();
    }

    private static PlantProfileRequestDto Payload(string commonName = "String of Pearls") => new()
    {
        CommonName = commonName,
        ScientificName = "Senecio rowleyanus",
        DefaultWateringIntervalDays = 12,
        LightRequirement = LightRequirement.Bright,
        HumidityNotes = "Prefers dry air.",
        CareTips = "Water sparingly; hang the pot so the strands drape.",
    };

    [Fact]
    public async Task Create_ValidProfile_Returns201_AndAppearsInList()
    {
        var created = await _client.PostAsJsonAsync("/api/plant-profiles", Payload(), Options);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var profile = await created.Content.ReadFromJsonAsync<PlantProfileResponseDto>(Options);
        Assert.NotNull(profile);

        var list = await _client.GetFromJsonAsync<List<PlantProfileResponseDto>>("/api/plant-profiles", Options);
        Assert.Contains(list!, p => p.Id == profile.Id && p.CommonName == "String of Pearls");
    }

    [Fact]
    public async Task Create_DuplicateCommonName_Returns409()
    {
        var first = await _client.PostAsJsonAsync("/api/plant-profiles", Payload(), Options);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/plant-profiles", Payload(), Options);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/plant-profiles", new
        {
            defaultWateringIntervalDays = 5,
        }, Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesProfile_AndIsReflectedInPlantDetailTips()
    {
        var profile = await CreateProfileAsync();
        var plant = await CreatePlantLinkedToAsync(profile.Id);

        var updated = await _client.PutAsJsonAsync($"/api/plant-profiles/{profile.Id}", new PlantProfileRequestDto
        {
            CommonName = profile.CommonName,
            ScientificName = null,
            DefaultWateringIntervalDays = 9,
            LightRequirement = LightRequirement.Medium,
            HumidityNotes = "Now likes steam.",
            CareTips = "# Water often\nFeed **monthly** in summer.",
        }, Options);

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var detail = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plant.Id}", Options);
        Assert.NotNull(detail?.CareTips);
        Assert.Equal("Now likes steam.", detail.CareTips.HumidityNotes);
        Assert.Equal("Medium", detail.CareTips.LightRequirement);
        Assert.Equal("# Water often\nFeed **monthly** in summer.", detail.CareTips.CareTips);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/plant-profiles/987654", Payload(), Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_NameTakenByAnotherProfile_Returns409()
    {
        await CreateProfileAsync("First");
        var second = await CreateProfileAsync("Second");

        var response = await _client.PutAsJsonAsync($"/api/plant-profiles/{second.Id}", Payload("First"), Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task List_IncludesFullDetails_AndPlantCount()
    {
        var created = await _client.PostAsJsonAsync("/api/plant-profiles", Payload("Counted Coral"), Options);
        var profile = await created.Content.ReadFromJsonAsync<PlantProfileResponseDto>(Options);
        await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Coral C",
            plantProfileId = profile!.Id,
        }, Options);

        var list = await _client.GetFromJsonAsync<List<PlantProfileResponseDto>>("/api/plant-profiles", Options);
        var row = list!.Single(p => p.Id == profile.Id);

        Assert.Equal(LightRequirement.Bright, row.LightRequirement);
        Assert.Equal("Prefers dry air.", row.HumidityNotes);
        Assert.Contains("Water sparingly", row.CareTips);
        Assert.Equal(1, row.PlantCount);
    }

    [Fact]
    public async Task Update_KeepsOwnName_ReturnsOk()
    {
        var profile = await CreateProfileAsync();

        var response = await _client.PutAsJsonAsync($"/api/plant-profiles/{profile.Id}", Payload(profile.CommonName), Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<PlantProfileResponseDto> CreateProfileAsync(string? commonName = null)
    {
        var created = await _client.PostAsJsonAsync("/api/plant-profiles", Payload(commonName ?? "String of Pearls"), Options);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantProfileResponseDto>(Options))!;
    }

    private async Task<PlantResponseDto> CreatePlantLinkedToAsync(int profileId)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Beads",
            location = "Shelf",
            plantProfileId = profileId,
        }, Options);

        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}
