using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using Xunit;

namespace PlantCare.Api.Tests;

public class PlantApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PlantApiTests()
    {
        _factory = _database.CreateFactory();
        _client = _factory.CreateClient();
    }

    private static DateTime Today => DateTime.UtcNow.Date;

    [Fact]
    public async Task List_EmptyDatabase_ReturnsEmptyArray()
    {
        var plants = await _client.GetFromJsonAsync<List<PlantResponseDto>>("/api/plants", Options);

        Assert.NotNull(plants);
        Assert.Empty(plants);
    }

    [Fact]
    public async Task Create_ValidPlant_Returns201_AndComputedDueStatus()
    {
        var created = await PostCreate(new
        {
            nickName = "Monstera Mike",
            customWateringIntervalDays = 7,
            lastWateredAt = Today.AddDays(-10),
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);

        var body = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(body);
        Assert.True(body.Id > 0);
        Assert.Equal("Monstera Mike", body.NickName);
        Assert.Equal(7, body.WateringIntervalDays);
        Assert.Equal(PlantDueStatus.Overdue, body.DueStatus);
        Assert.Equal(-3, body.DaysUntilDue);
        Assert.Equal("3 days overdue", body.DueMessage);
    }

    [Fact]
    public async Task Create_LinkedProfile_UsesProfileInterval()
    {
        var profileId = await GetAnyProfileIdAsync();

        var created = await PostCreate(new
        {
            nickName = "Potted Fig",
            plantProfileId = profileId,
            lastWateredAt = Today,
        });

        var body = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(body);
        Assert.Equal(profileId, body.PlantProfileId);
        Assert.NotNull(body.ProfileCommonName);
        Assert.Equal(PlantDueStatus.Upcoming, body.DueStatus);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        var response = await PostCreate(new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_IntervalOutOfRange_Returns400()
    {
        var response = await PostCreate(new
        {
            nickName = "Bad",
            customWateringIntervalDays = 0,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownProfile_Returns400()
    {
        var response = await PostCreate(new
        {
            nickName = "Ghost",
            plantProfileId = 987654,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndUpdate_CarryRepotLifecycleFields()
    {
        var created = await PostCreate(new
        {
            nickName = "Repot Rita",
            potSizeCm = 14,
            soilMix = "Bark, perlite, coco",
            propagatedFrom = "Cutting from grandma's plant",
        });
        var body = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.Equal(14, body!.PotSizeCm);
        Assert.Equal("Bark, perlite, coco", body.SoilMix);
        Assert.Equal("Cutting from grandma's plant", body.PropagatedFrom);

        var response = await _client.PutAsJsonAsync($"/api/plants/{body.Id}", new
        {
            nickName = "Repot Rita",
            potSizeCm = 18,
            soilMix = "Fresh aroid mix",
            propagatedFrom = null as string,
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.Equal(18, updated!.PotSizeCm);
        Assert.Equal("Fresh aroid mix", updated.SoilMix);
        Assert.Null(updated.PropagatedFrom);
    }

    [Fact]
    public async Task Create_UnknownRoom_Returns400()
    {
        var response = await PostCreate(new
        {
            nickName = "Ghost",
            roomId = 987654,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/plants/424242");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesFields_AndReturnsOk()
    {
        var room = await _client.PostAsJsonAsync("/api/rooms", new { name = "Windowsill" }, Options);
        var roomBody = await room.Content.ReadFromJsonAsync<RoomResponseDto>(Options);
        Assert.NotNull(roomBody);

        var created = await PostCreate(new { nickName = "Before", roomId = roomBody.Id });
        var createdBody = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(createdBody);

        var response = await _client.PutAsJsonAsync($"/api/plants/{createdBody.Id}", new
        {
            nickName = "After",
            roomId = roomBody.Id,
            customWateringIntervalDays = 5,
            lastWateredAt = Today,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(updated);
        Assert.Equal("After", updated.NickName);
        Assert.Equal("Windowsill", updated.RoomName);
        Assert.Equal(5, updated.WateringIntervalDays);

        var fetched = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{createdBody.Id}", Options);
        Assert.Equal("After", fetched?.NickName);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/plants/654321", new
        {
            nickName = "Nope",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesPlant()
    {
        var created = await PostCreate(new { nickName = "Doomed" });
        var body = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(body);

        var response = await _client.DeleteAsync($"/api/plants/{body.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var after = await _client.GetAsync($"/api/plants/{body.Id}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/plants/999111");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostCreate(object payload)
        => _client.PostAsJsonAsync("/api/plants", payload, Options);

    private async Task<int> GetAnyProfileIdAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.PlantProfiles.Select(p => p.Id).FirstAsync();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
