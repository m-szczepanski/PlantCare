using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using Xunit;

namespace PlantCare.Api.Tests;

public class PlantNotesApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public PlantNotesApiTests()
    {
        var factory = _database.CreateFactory();
        _client = factory.CreateClient();
    }

    private async Task<int> CreatePlant()
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Noted Nora",
            location = "Kitchen",
        }, Options);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!.Id;
    }

    [Fact]
    public async Task AddNote_ThenListed_NewestFirst()
    {
        var id = await CreatePlant();

        var first = await _client.PostAsJsonAsync($"/api/plants/{id}/notes", new { text = "New leaf spotted!" }, Options);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var second = await _client.PostAsJsonAsync($"/api/plants/{id}/notes", new { text = "Slight yellowing" }, Options);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var notes = await _client.GetFromJsonAsync<List<PlantNoteResponseDto>>($"/api/plants/{id}/notes", Options);

        Assert.Equal(2, notes!.Count);
        Assert.Equal("Slight yellowing", notes[0].Text);
        Assert.True(notes[0].CreatedAt >= notes[1].CreatedAt);
    }

    [Fact]
    public async Task AddNote_EmptyText_Returns400()
    {
        var id = await CreatePlant();

        var response = await _client.PostAsJsonAsync($"/api/plants/{id}/notes", new { text = "" }, Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Notes_UnknownPlant_Returns404()
    {
        var list = await _client.GetAsync("/api/plants/424242/notes");
        var add = await _client.PostAsJsonAsync("/api/plants/424242/notes", new { text = "hello" }, Options);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, add.StatusCode);
    }

    [Fact]
    public async Task DeletePlant_RemovesNotes()
    {
        var id = await CreatePlant();
        await _client.PostAsJsonAsync($"/api/plants/{id}/notes", new { text = "gone with the plant" }, Options);

        await _client.DeleteAsync($"/api/plants/{id}");

        var orphans = await _client.GetAsync($"/api/plants/{id}/notes");
        Assert.Equal(HttpStatusCode.NotFound, orphans.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}
