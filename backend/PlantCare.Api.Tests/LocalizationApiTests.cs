using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;
using PlantCare.Api.Services.Localization;
using Xunit;

namespace PlantCare.Api.Tests;

public class LocalizationApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private sealed class RecordingPublisher : INtfyPublisher
    {
        public List<(string Title, string Message)> Published { get; } = [];

        public Task PublishAsync(string title, string message, int priority = 3, string? clickUrl = null, string? buttonLabel = null, string? buttonUrl = null, CancellationToken cancellationToken = default)
        {
            Published.Add((title, message));
            LastButtonLabel = buttonLabel;
            return Task.CompletedTask;
        }

        public string? LastButtonLabel { get; private set; }
    }

    private static DateTime Today => DateTime.UtcNow.Date;

    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LocalizationApiTests()
    {
        _factory = _database.CreateFactory();
        _client = _factory.CreateClient();
    }

    private HttpClient PolishClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("pl"));
        return client;
    }

    private async Task<PlantResponseDto> CreatePlantAsync(HttpClient client, string nickName, int intervalDays, int daysSinceWatered)
    {
        var created = await client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            customWateringIntervalDays = intervalDays,
            lastWateredAt = Today.AddDays(-daysSinceWatered),
        }, Options);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    [Fact]
    public async Task Plants_WithoutAcceptLanguage_UseEnglishMessages()
    {
        await CreatePlantAsync(_client, "Thirsty", 7, 10);

        var plants = await _client.GetFromJsonAsync<List<PlantResponseDto>>("/api/plants", Options);

        Assert.Equal("3 days overdue", Assert.Single(plants!).DueMessage);
    }

    [Fact]
    public async Task Plants_WithPolishAcceptLanguage_LocalizesDueMessage()
    {
        var client = PolishClient();
        await CreatePlantAsync(client, "Spragniona", 7, 10);

        var plants = await client.GetFromJsonAsync<List<PlantResponseDto>>("/api/plants", Options);

        Assert.Equal("Zaległe o 3 dni", Assert.Single(plants!).DueMessage);
    }

    [Fact]
    public async Task Plants_WithPolishAcceptLanguage_LocalizesProfileAndCareTips()
    {
        var profiles = await _client.GetFromJsonAsync<List<PlantProfileResponseDto>>("/api/plant-profiles", Options);
        var snakePlant = profiles!.Single(p => p.CommonName == "Snake Plant");
        var client = PolishClient();
        var created = await client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Grażyna",
            plantProfileId = snakePlant.Id,
            customWateringIntervalDays = 14,
            lastWateredAt = Today.AddDays(-1),
        }, Options);
        created.EnsureSuccessStatusCode();

        var plant = (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;

        Assert.Equal("Sansewieria", plant.CareTips!.CommonName);
        Assert.Contains("suche powietrze", plant.CareTips.HumidityNotes);
        Assert.Equal("Sansewieria", plant.ProfileCommonName);
    }

    [Fact]
    public async Task PlantProfiles_PolishRequest_ServesSeededTranslations_EnglishStaysCanonical()
    {
        var polish = await PolishClient().GetFromJsonAsync<List<PlantProfileResponseDto>>("/api/plant-profiles", Options);
        var english = await _client.GetFromJsonAsync<List<PlantProfileResponseDto>>("/api/plant-profiles", Options);

        Assert.Contains("Sansewieria", polish!.Select(p => p.CommonName));
        Assert.DoesNotContain("Sansewieria", english!.Select(p => p.CommonName));
        Assert.Contains("Snake Plant", english!.Select(p => p.CommonName));
        Assert.DoesNotContain("Snake Plant", polish!.Select(p => p.CommonName));
    }

    [Fact]
    public async Task DuplicateRoom_PolishRequest_LocalizesProblemDetails()
    {
        var client = PolishClient();
        await client.PostAsJsonAsync("/api/rooms", new { name = "Salon" }, Options);

        var response = await client.PostAsJsonAsync("/api/rooms", new { name = "Salon" }, Options);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Pokój o tej nazwie już istnieje.", body);
    }

    [Fact]
    public async Task Digest_WithPolishDefaultLanguage_LocalizesTitleBodyAndButton()
    {
        var publisher = new RecordingPublisher();
        using var plFactory = _database.CreateFactory(
            configureBuilder: builder => builder.UseSetting("APP_LANGUAGE", "pl"),
            configureTestServices: services =>
            {
                services.Remove(services.Single(d => d.ServiceType == typeof(INtfyPublisher)));
                services.AddSingleton<INtfyPublisher>(publisher);
            });

        var client = plFactory.CreateClient();
        await CreatePlantAsync(client, "Bazylia", 7, 30);

        await using var scope = plFactory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IWateringCheckService>().RunAsync();

        var digest = Assert.Single(publisher.Published);
        Assert.Equal("1 roślina potrzebuje podlania (zalega 1)", digest.Title);
        Assert.Contains("• Bazylia", digest.Message);
        Assert.Contains("Zaległe o 23 dni", digest.Message);
        Assert.Equal("Podlej teraz", publisher.LastButtonLabel);
    }

    [Fact]
    public void Messages_PolishPluralCategories_FollowCardinalRules()
    {
        Assert.Equal("1 roślina potrzebuje podlania", Messages.GetPlural("pl", "digest.title", 1));
        Assert.Equal("2 rośliny potrzebują podlania", Messages.GetPlural("pl", "digest.title", 2));
        Assert.Equal("5 roślin potrzebuje podlania", Messages.GetPlural("pl", "digest.title", 5));
        Assert.Equal("12 roślin potrzebuje podlania", Messages.GetPlural("pl", "digest.title", 12));
        Assert.Equal("22 rośliny potrzebują podlania", Messages.GetPlural("pl", "digest.title", 22));
        Assert.Equal("3 days overdue", Messages.GetPlural("en", "due.overdue", 3));
        Assert.Equal("1 day overdue", Messages.GetPlural("en", "due.overdue", 1));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
