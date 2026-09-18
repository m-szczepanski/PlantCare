using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Data;
using PlantCare.Api.Models;
using Xunit;

namespace PlantCare.Api.Tests;

public class DatabaseBootTests : IDisposable
{
    private readonly TempDatabase _database = new();

    [Fact]
    public async Task Startup_CreatesSchemaAndSeedsProfiles()
    {
        using var factory = _database.CreateFactory();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tables = await GetTableNamesAsync(db);
        Assert.Contains("Plants", tables);
        Assert.Contains("PlantProfiles", tables);
        Assert.Contains("WateringLogs", tables);
        Assert.Contains("NotificationLogs", tables);

        Assert.NotEmpty(await db.PlantProfiles.ToListAsync());
    }

    [Fact]
    public async Task Startup_RunsTwice_DoesNotDuplicateSeedRows()
    {
        int firstCount;
        using (var factory = _database.CreateFactory())
        {
            await using var scope = factory.Services.CreateAsyncScope();
            firstCount = await scope.ServiceProvider.GetRequiredService<AppDbContext>().PlantProfiles.CountAsync();
        }

        using (var factory = _database.CreateFactory())
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.Equal(firstCount, await db.PlantProfiles.CountAsync());
        }
    }

    [Fact]
    public async Task Startup_StoredLightRequirementAsText()
    {
        using var factory = _database.CreateFactory();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT ""LightRequirement"" FROM ""PlantProfiles"" LIMIT 1";
        var stored = (string?)await command.ExecuteScalarAsync();

        Assert.NotNull(stored);
        Assert.Contains(stored, Enum.GetNames<LightRequirement>());
    }

    private static async Task<List<string>> GetTableNamesAsync(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT name FROM sqlite_master WHERE type = 'table'";
        await using var reader = await command.ExecuteReaderAsync();

        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    public void Dispose() => _database.Dispose();
}
