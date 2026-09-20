using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class JournalApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly TempDatabase _database = new();
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"plantcare-journal-{Guid.NewGuid():N}");
    private readonly HttpClient _client;

    public JournalApiTests()
    {
        var factory = _database.CreateFactory(configureBuilder: builder =>
            builder.UseSetting("PHOTO_STORAGE_PATH", _storagePath));
        _client = factory.CreateClient();
    }

    private async Task<int> CreatePlant()
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new { nickName = "Journal Jo" }, Options);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!.Id;
    }

    private async Task<HttpResponseMessage> PostEntry(int plantId, string? date = null, string? text = null, byte[]? photo = null)
    {
        var form = new MultipartFormDataContent();
        if (date is not null)
        {
            form.Add(new StringContent(date), "entryDate");
        }

        if (text is not null)
        {
            form.Add(new StringContent(text), "text");
        }

        if (photo is not null)
        {
            var content = new ByteArrayContent(photo);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(content, "file", "entry.png");
        }

        return await _client.PostAsync($"/api/plants/{plantId}/journal", form);
    }

    [Fact]
    public async Task CreateWithPhotoAndText_ListedNewestFirst_AndStored()
    {
        var plantId = await CreatePlant();

        var first = await PostEntry(plantId, "2026-03-01T00:00:00", "Small cutting");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var second = await PostEntry(plantId, "2026-06-01T00:00:00", "Big leaves now", Png);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var createdEntry = (await second.Content.ReadFromJsonAsync<JournalEntryResponseDto>(Options))!;
        Assert.NotNull(createdEntry.PhotoUrl);
        Assert.Contains("/journal/", createdEntry.PhotoUrl);
        Assert.True(File.Exists(Path.Combine(_storagePath, createdEntry.PhotoUrl[PlantPhotoStorage.PublicUrlPrefix.Length..])));

        var entries = await _client.GetFromJsonAsync<List<JournalEntryResponseDto>>($"/api/plants/{plantId}/journal", Options);

        Assert.Equal(2, entries!.Count);
        Assert.Equal("Big leaves now", entries[0].Text);
        Assert.Equal("Small cutting", entries[1].Text);
    }

    [Fact]
    public async Task CreateWithoutContent_ReturnsBadRequest()
    {
        var plantId = await CreatePlant();

        var response = await PostEntry(plantId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownPlant_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await PostEntry(424242, text: "hello")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/plants/424242/journal")).StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesEntryAndPhoto()
    {
        var plantId = await CreatePlant();
        var created = await PostEntry(plantId, text: "temp", photo: Png);
        var entry = (await created.Content.ReadFromJsonAsync<JournalEntryResponseDto>(Options))!;
        var path = Path.Combine(_storagePath, entry.PhotoUrl![PlantPhotoStorage.PublicUrlPrefix.Length..]);

        var delete = await _client.DeleteAsync($"/api/plants/{plantId}/journal/{entry.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.False(File.Exists(path));
        var entries = await _client.GetFromJsonAsync<List<JournalEntryResponseDto>>($"/api/plants/{plantId}/journal", Options);
        Assert.Empty(entries!);
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
        try
        {
            Directory.Delete(_storagePath, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
