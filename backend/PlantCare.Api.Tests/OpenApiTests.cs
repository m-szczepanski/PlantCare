using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PlantCare.Api.Tests;

public class OpenApiTests : IDisposable
{
    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public OpenApiTests()
    {
        _client = _database.CreateFactory().CreateClient();
    }

    [Fact]
    public async Task OpenApiDocument_ServedWithAllRouteGroups()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("/api/plants", body);
        Assert.Contains("/api/rooms", body);
        Assert.Contains("/api/dashboard", body);
        Assert.Contains("/api/export", body);
        Assert.Contains("/api/calendar.ics", body);
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}
