using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using Xunit;

namespace PlantCare.Api.Tests;

public class RoomApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public RoomApiTests()
    {
        var factory = _database.CreateFactory();
        _client = factory.CreateClient();
    }

    private async Task<RoomResponseDto> CreateRoom(string name, RoomOrientation? orientation = null)
    {
        var response = await _client.PostAsJsonAsync("/api/rooms", new { name, orientation }, Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var room = await response.Content.ReadFromJsonAsync<RoomResponseDto>(Options);
        Assert.NotNull(room);
        return room;
    }

    [Fact]
    public async Task CreateRoom_ReturnsRoom_WithOrientation()
    {
        var room = await CreateRoom("Living room", RoomOrientation.South);

        Assert.Equal("Living room", room.Name);
        Assert.Equal(RoomOrientation.South, room.Orientation);
        Assert.Equal(0, room.PlantCount);
    }

    [Fact]
    public async Task CreateRoom_DuplicateName_ReturnsConflict()
    {
        await CreateRoom("Kitchen");

        var response = await _client.PostAsJsonAsync("/api/rooms", new { name = "Kitchen" }, Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task List_CountsPlants_AndUpdateRenames()
    {
        var room = await CreateRoom("Study");
        await _client.PostAsJsonAsync("/api/plants", new { nickName = "Desk Dan", roomId = room.Id }, Options);

        var rooms = await _client.GetFromJsonAsync<List<RoomResponseDto>>("/api/rooms", Options);
        Assert.Equal(1, rooms!.Single(r => r.Id == room.Id).PlantCount);

        var update = await _client.PutAsJsonAsync($"/api/rooms/{room.Id}", new { name = "Library", orientation = (RoomOrientation?)null }, Options);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var renamed = await update.Content.ReadFromJsonAsync<RoomResponseDto>(Options);
        Assert.Equal("Library", renamed!.Name);
        Assert.Null(renamed.Orientation);
    }

    [Fact]
    public async Task Delete_SetsPlantRoomIdNull()
    {
        var room = await CreateRoom("Attic");
        var created = await _client.PostAsJsonAsync("/api/plants", new { nickName = "Attic Andy", roomId = room.Id }, Options);
        var plant = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        var delete = await _client.DeleteAsync($"/api/rooms/{room.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var fetched = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plant!.Id}", Options);
        Assert.Null(fetched!.RoomId);
        Assert.Null(fetched.RoomName);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/rooms/424242");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PlantResponse_IncludesRoomName()
    {
        var room = await CreateRoom("Balcony");
        var created = await _client.PostAsJsonAsync("/api/plants", new { nickName = "Figgy", roomId = room.Id }, Options);
        var plant = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.Equal(room.Id, plant!.RoomId);
        Assert.Equal("Balcony", plant.RoomName);
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}
