using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class PlantPhotoUploadApiTests : IDisposable
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"plantcare-photos-{Guid.NewGuid():N}");
    private readonly HttpClient _client;

    public PlantPhotoUploadApiTests()
    {
        var factory = _database.CreateFactory(configureBuilder: builder => builder.UseSetting("PHOTO_STORAGE_PATH", _storagePath));
        _client = factory.CreateClient();
    }

    public void Dispose()
    {
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
        GC.SuppressFinalize(this);
    }

    private async Task<int> CreatePlant(string nickName = "Monstera Mike")
    {
        var response = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            location = "Living room",
            customWateringIntervalDays = 7,
        });
        response.EnsureSuccessStatusCode();
        var plant = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        return plant!.Id;
    }

    private async Task<HttpResponseMessage> Upload(int plantId, byte[] bytes, string contentType)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fileContent, "file", "photo.png");
        return await _client.PostAsync($"/api/plants/{plantId}/photo", form);
    }

    [Fact]
    public async Task UploadValidPng_StoresFileAndUpdatesPlantPhotoUrl()
    {
        var plantId = await CreatePlant();

        var response = await Upload(plantId, PngBytes, "image/png");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plant = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(plant!.PhotoUrl);
        Assert.StartsWith($"/uploads/plants/{plantId}/", plant.PhotoUrl);

        var stored = Path.Combine(_storagePath, "plants", plantId.ToString(), Path.GetFileName(plant.PhotoUrl));
        Assert.True(File.Exists(stored));
        Assert.Equal(PngBytes, await File.ReadAllBytesAsync(stored));
    }

    [Fact]
    public async Task UploadReplacesPreviousPhotoAndDeletesOldManagedFile()
    {
        var plantId = await CreatePlant();

        var first = await Upload(plantId, PngBytes, "image/png");
        var firstUrl = (await first.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!.PhotoUrl;
        var firstPath = Path.Combine(_storagePath, "plants", plantId.ToString(), Path.GetFileName(firstUrl!));
        Assert.True(File.Exists(firstPath));

        var second = await Upload(plantId, [.. PngBytes, 0x01], "image/png");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.False(File.Exists(firstPath));
    }

    [Fact]
    public async Task UploadUnsupportedContentType_ReturnsBadRequest()
    {
        var plantId = await CreatePlant();

        var response = await Upload(plantId, [1, 2, 3], "application/pdf");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unsupported photo type", body);
        Assert.False(Directory.Exists(Path.Combine(_storagePath, "plants", plantId.ToString())));
    }

    [Fact]
    public async Task UploadWithoutFile_ReturnsBadRequest()
    {
        var plantId = await CreatePlant();

        var response = await _client.PostAsync($"/api/plants/{plantId}/photo", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadToUnknownPlant_ReturnsNotFound()
    {
        var response = await Upload(999, PngBytes, "image/png");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletePlant_RemovesManagedPhotoDirectory()
    {
        var plantId = await CreatePlant();
        await Upload(plantId, PngBytes, "image/png");
        var directory = Path.Combine(_storagePath, "plants", plantId.ToString());
        Assert.True(Directory.Exists(directory));

        var response = await _client.DeleteAsync($"/api/plants/{plantId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(Directory.Exists(directory));
    }
}
