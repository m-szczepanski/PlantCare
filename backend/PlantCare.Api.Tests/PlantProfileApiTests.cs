using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using Xunit;

namespace PlantCare.Api.Tests;

public class PlantProfileApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PlantProfileApiTests()
    {
        _factory = _database.CreateFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task List_ReturnsSeededProfiles()
    {
        var profiles = await _client.GetFromJsonAsync<List<PlantProfileResponseDto>>("/api/plant-profiles", Options);

        Assert.NotNull(profiles);
        Assert.NotEmpty(profiles);
        Assert.Contains(profiles, p => p.CommonName == "Monstera");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
