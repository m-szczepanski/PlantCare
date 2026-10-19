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

        var first = await _client.PostAsJsonAsync(
            $"/api/plants/{id}/notes",
            new { text = "New leaf spotted!" },
            Options);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var second = await _client.PostAsJsonAsync(
            $"/api/plants/{id}/notes",
            new { text = "Slight yellowing" },
            Options);
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
    public async Task UpdateNote_ReplacesText_AndKeepsPin()
    {
        var id = await CreatePlant();
        var created = await _client.PostAsJsonAsync(
            $"/api/plants/{id}/notes", new { text = "Original text" }, Options);
        var note = await created.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);
        await _client.PutAsJsonAsync(
            $"/api/plants/{id}/notes/{note!.Id}/pin", new { pinned = true }, Options);

        var response = await _client.PutAsJsonAsync(
            $"/api/plants/{id}/notes/{note.Id}", new { text = "  Corrected text  " }, Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);
        Assert.Equal("Corrected text", updated!.Text);
        Assert.True(updated.IsPinned);
        Assert.Equal(note.CreatedAt, updated.CreatedAt);

        var notes = await _client.GetFromJsonAsync<List<PlantNoteResponseDto>>($"/api/plants/{id}/notes", Options);
        Assert.Single(notes!);
        Assert.Equal("Corrected text", notes![0].Text);
    }

    [Fact]
    public async Task UpdateNote_EmptyText_Returns400()
    {
        var id = await CreatePlant();
        var created = await _client.PostAsJsonAsync(
            $"/api/plants/{id}/notes", new { text = "Keep me" }, Options);
        var note = await created.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);

        var response = await _client.PutAsJsonAsync(
            $"/api/plants/{id}/notes/{note!.Id}", new { text = "" }, Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateNote_UnknownNoteOrWrongPlant_Returns404()
    {
        var id = await CreatePlant();
        var otherId = await CreatePlant();
        var created = await _client.PostAsJsonAsync(
            $"/api/plants/{id}/notes", new { text = "Mine" }, Options);
        var note = await created.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);

        var wrongPlant = await _client.PutAsJsonAsync(
            $"/api/plants/{otherId}/notes/{note!.Id}", new { text = "Hijacked" }, Options);
        var missing = await _client.PutAsJsonAsync(
            $"/api/plants/{id}/notes/424242", new { text = "Ghost" }, Options);

        Assert.Equal(HttpStatusCode.NotFound, wrongPlant.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task DeleteNote_RemovesOnlyThatNote()
    {
        var id = await CreatePlant();
        var keep = await _client.PostAsJsonAsync(
            $"/api/plants/{id}/notes", new { text = "Keep" }, Options);
        var keepNote = await keep.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);
        var drop = await _client.PostAsJsonAsync(
            $"/api/plants/{id}/notes", new { text = "Drop" }, Options);
        var dropNote = await drop.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);

        var response = await _client.DeleteAsync($"/api/plants/{id}/notes/{dropNote!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var notes = await _client.GetFromJsonAsync<List<PlantNoteResponseDto>>($"/api/plants/{id}/notes", Options);
        Assert.Single(notes!);
        Assert.Equal(keepNote!.Id, notes![0].Id);
    }

    [Fact]
    public async Task DeleteNote_UnknownNoteOrWrongPlant_Returns404()
    {
        var id = await CreatePlant();
        var otherId = await CreatePlant();
        var created = await _client.PostAsJsonAsync(
            $"/api/plants/{id}/notes", new { text = "Mine" }, Options);
        var note = await created.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);

        var wrongPlant = await _client.DeleteAsync($"/api/plants/{otherId}/notes/{note!.Id}");
        var missing = await _client.DeleteAsync($"/api/plants/{id}/notes/424242");

        Assert.Equal(HttpStatusCode.NotFound, wrongPlant.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var notes = await _client.GetFromJsonAsync<List<PlantNoteResponseDto>>($"/api/plants/{id}/notes", Options);
        Assert.Single(notes!);
    }

    [Fact]
    public async Task PinNote_PutsItFirst_AndUnpinRestoresNewestFirst()
    {
        var id = await CreatePlant();
        var first = await _client.PostAsJsonAsync(
            $"/api/plants/{id}/notes",
            new { text = "Keep this visible" },
            Options);
        var firstNote = await first.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);
        await _client.PostAsJsonAsync($"/api/plants/{id}/notes", new { text = "Regular note" }, Options);

        var pinned = await _client.PutAsJsonAsync(
            $"/api/plants/{id}/notes/{firstNote!.Id}/pin",
            new { pinned = true },
            Options);
        Assert.Equal(HttpStatusCode.OK, pinned.StatusCode);
        var pinnedBody = await pinned.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);
        Assert.True(pinnedBody!.IsPinned);

        var notes = await _client.GetFromJsonAsync<List<PlantNoteResponseDto>>($"/api/plants/{id}/notes", Options);
        Assert.Equal("Keep this visible", notes![0].Text);
        Assert.True(notes[0].IsPinned);
        Assert.False(notes[1].IsPinned);

        var unpinned = await _client.PutAsJsonAsync(
            $"/api/plants/{id}/notes/{firstNote.Id}/pin",
            new { pinned = false },
            Options);
        var unpinnedBody = await unpinned.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);
        Assert.False(unpinnedBody!.IsPinned);

        var reordered = await _client.GetFromJsonAsync<List<PlantNoteResponseDto>>(
            $"/api/plants/{id}/notes", Options);
        Assert.Equal("Regular note", reordered![0].Text);
    }

    [Fact]
    public async Task PinNote_UnknownNoteOrWrongPlant_Returns404()
    {
        var id = await CreatePlant();
        var otherId = await CreatePlant();
        var created = await _client.PostAsJsonAsync(
            $"/api/plants/{id}/notes",
            new { text = "Mine" },
            Options);
        var note = await created.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options);

        var wrongPlant = await _client.PutAsJsonAsync(
            $"/api/plants/{otherId}/notes/{note!.Id}/pin",
            new { pinned = true },
            Options);
        var missing = await _client.PutAsJsonAsync(
            $"/api/plants/{id}/notes/424242/pin",
            new { pinned = true },
            Options);

        Assert.Equal(HttpStatusCode.NotFound, wrongPlant.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PinNote_BeyondThree_Returns400_AndKeepsThree()
    {
        var id = await CreatePlant();
        var noteIds = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            var response = await _client.PostAsJsonAsync(
                $"/api/plants/{id}/notes", new { text = $"Note {i}" }, Options);
            noteIds.Add((await response.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options))!.Id);
        }

        foreach (var noteId in noteIds.Take(3))
        {
            var pinned = await _client.PutAsJsonAsync(
                $"/api/plants/{id}/notes/{noteId}/pin", new { pinned = true }, Options);
            Assert.Equal(HttpStatusCode.OK, pinned.StatusCode);
        }

        var fourth = await _client.PutAsJsonAsync(
            $"/api/plants/{id}/notes/{noteIds[3]}/pin", new { pinned = true }, Options);
        Assert.Equal(HttpStatusCode.BadRequest, fourth.StatusCode);

        var again = await _client.PutAsJsonAsync(
            $"/api/plants/{id}/notes/{noteIds[0]}/pin", new { pinned = true }, Options);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        var notes = await _client.GetFromJsonAsync<List<PlantNoteResponseDto>>($"/api/plants/{id}/notes", Options);
        Assert.Equal(3, notes!.Count(n => n.IsPinned));
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
        GC.SuppressFinalize(this);
    }
}
