using Microsoft.AspNetCore.Http;
using Xunit;

namespace PlantCare.Api.Tests;

public class HealthEndpointTests : IDisposable
{
    private readonly TempDatabase _database = new();

    [Fact]
    public async Task Get_Returns200WithoutDatabase()
    {
        using var factory = _database.CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    public void Dispose() => _database.Dispose();
}
