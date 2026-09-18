using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using Xunit;

namespace PlantCare.Api.Tests;

public class DashboardApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DashboardApiTests()
    {
        _factory = _database.CreateFactory();
        _client = _factory.CreateClient();
    }

    private static DateTime Today => DateTime.UtcNow.Date;

    [Fact]
    public async Task Get_EmptyDatabase_ReturnsThreeEmptyBuckets()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponseDto>("/api/dashboard", Options);

        Assert.NotNull(dashboard);
        Assert.Empty(dashboard.Overdue);
        Assert.Empty(dashboard.DueToday);
        Assert.Empty(dashboard.Upcoming);
    }

    [Fact]
    public async Task Get_GroupsPlantsIntoBucketsMatchingDueStatus()
    {
        var veryOverdue = await CreatePlant("Very Overdue", intervalDays: 7, lastWateredDaysAgo: 17);
        var slightlyOverdue = await CreatePlant("Slightly Overdue", intervalDays: 7, lastWateredDaysAgo: 9);
        var dueToday = await CreatePlant("Due Today", intervalDays: 7, lastWateredDaysAgo: 7);
        var upcoming = await CreatePlant("Upcoming", intervalDays: 7, lastWateredDaysAgo: 1);
        var unscheduled = await CreatePlant("Unscheduled", intervalDays: null, lastWateredDaysAgo: null);

        var response = await _client.GetAsync("/api/dashboard");
        response.EnsureSuccessStatusCode();
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponseDto>(Options);

        Assert.NotNull(dashboard);
        Assert.Equal([veryOverdue.Id, slightlyOverdue.Id], dashboard.Overdue.Select(p => p.Id));
        Assert.Equal([dueToday.Id], dashboard.DueToday.Select(p => p.Id));
        Assert.Equal([upcoming.Id, unscheduled.Id], dashboard.Upcoming.Select(p => p.Id));

        var all = dashboard.Overdue.Concat(dashboard.DueToday).Concat(dashboard.Upcoming).ToList();
        Assert.Equal(5, all.Count);
        foreach (var plant in all)
        {
            var detail = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plant.Id}", Options);
            Assert.Equal(plant.DueStatus, detail!.DueStatus);
        }
    }

    private async Task<PlantResponseDto> CreatePlant(string nickName, int? intervalDays, int? lastWateredDaysAgo)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            location = "Desk",
            customWateringIntervalDays = intervalDays,
            lastWateredAt = lastWateredDaysAgo is null ? (DateTime?)null : Today.AddDays(-lastWateredDaysAgo.Value),
        }, Options);

        var plant = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.NotNull(plant);
        return plant;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
