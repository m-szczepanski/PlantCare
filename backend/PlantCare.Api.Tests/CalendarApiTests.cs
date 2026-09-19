using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using Xunit;

namespace PlantCare.Api.Tests;

public class CalendarApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CalendarApiTests()
    {
        _factory = _database.CreateFactory();
        _client = _factory.CreateClient();
    }

    private async Task<int> CreatePlant(string nickName, int? interval)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            location = "Kitchen",
            customWateringIntervalDays = interval,
            lastWateredAt = DateTime.UtcNow.AddDays(-3),
        });
        var plant = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        return plant!.Id;
    }

    [Fact]
    public async Task Feed_WithScheduledPlants_ReturnsValidIcalendarDocument()
    {
        var id = await CreatePlant("Monstera Mike", 7);

        var response = await _client.GetAsync("/api/calendar.ics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/calendar", response.Content.Headers.ContentType!.ToString());
        Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

        var feed = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("BEGIN:VCALENDAR\r\n", feed);
        Assert.EndsWith("END:VCALENDAR\r\n", feed);
        Assert.Contains($"UID:plant-{id}@plantcare", feed);
        Assert.Contains("SUMMARY:Water Monstera Mike", feed);
        Assert.Contains("RRULE:FREQ=DAILY;INTERVAL=7", feed);
        Assert.Contains("DTSTART;VALUE=DATE:", feed);
        Assert.DoesNotContain("\n", feed.Replace("\r\n", ""));
    }

    [Fact]
    public async Task Feed_SkipsUnscheduledPlants()
    {
        await CreatePlant("Saguaro Sam", null);

        var feed = await _client.GetStringAsync("/api/calendar.ics");

        Assert.DoesNotContain("Saguaro Sam", feed);
    }

    [Fact]
    public async Task Feed_EscapesReservedCharactersInSummary()
    {
        await CreatePlant("A, B; C", 5);

        var feed = await _client.GetStringAsync("/api/calendar.ics");

        Assert.Contains("SUMMARY:Water A\\, B\\; C", feed.Replace("\r\n ", ""));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
