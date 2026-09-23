using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using Xunit;

namespace PlantCare.Api.Tests;

public class HealthCheckApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static DateTime Today => DateTime.UtcNow.Date;

    private readonly TempDatabase _database = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckApiTests()
    {
        _factory = _database.CreateFactory();
        _client = _factory.CreateClient();
    }

    private async Task<PlantResponseDto> CreatePlant(string nickName, int intervalDays = 7, int daysSinceWatered = 0, int daysSinceAcquired = 60)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            customWateringIntervalDays = intervalDays,
            lastWateredAt = Today.AddDays(-daysSinceWatered),
            acquiredDate = Today.AddDays(-daysSinceAcquired),
        }, Options);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    private async Task<HttpResponseMessage> Checkup(int plantId, object body)
        => await _client.PostAsJsonAsync($"/api/plants/{plantId}/health-checks", body, Options);

    [Fact]
    public async Task AddHealthCheck_UpdatesPlantStatus_AndReschedulesWatering()
    {
        var plant = await CreatePlant("Wilting Walter", intervalDays: 7, daysSinceWatered: 5);
        Assert.Equal(2, plant.DaysUntilDue);
        Assert.True(plant.CheckupDue);

        var response = await Checkup(plant.Id, new { status = "Sick", note = "Mushy stem" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.Equal(HealthStatus.Sick, updated!.HealthStatus);
        Assert.NotNull(updated.LastCheckupAt);
        Assert.False(updated.CheckupDue);
        // 7-day schedule -> sick x1.5 = 11 days; watered 5 days ago -> 6 to go.
        Assert.Equal(11, updated.WateringIntervalDays);
        Assert.Equal(6, updated.DaysUntilDue);
    }

    [Fact]
    public async Task AddHealthCheck_Excellent_BringsWateringForward()
    {
        var plant = await CreatePlant("Thriving Tilly", intervalDays: 10, daysSinceWatered: 5);

        var response = await Checkup(plant.Id, new { status = "Excellent" });
        var updated = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);

        Assert.Equal(8, updated!.WateringIntervalDays);
        Assert.Equal(3, updated.DaysUntilDue);
    }

    [Fact]
    public async Task HealthChecks_ListReturnsNewestFirst()
    {
        var plant = await CreatePlant("Chatty Charles");
        await Checkup(plant.Id, new { status = "Bad", note = "First" });
        await Checkup(plant.Id, new { status = "Good", note = "Second" });

        var list = await _client.GetFromJsonAsync<List<HealthCheckResponseDto>>($"/api/plants/{plant.Id}/health-checks", Options);

        Assert.Equal(2, list!.Count);
        Assert.Equal(HealthStatus.Good, list[0].Status);
        Assert.Equal("Second", list[0].Note);
        Assert.Equal(HealthStatus.Bad, list[1].Status);
    }

    [Fact]
    public async Task CheckupDue_FlagTracksAcquisitionAndAnswers()
    {
        var fresh = await CreatePlant("New Nancy", daysSinceAcquired: 5);
        Assert.False(fresh.CheckupDue);

        var mature = await CreatePlant("Mature Marge", daysSinceAcquired: 31);
        Assert.True(mature.CheckupDue);

        var response = await Checkup(mature.Id, new { status = "Good" });
        var updated = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.False(updated!.CheckupDue);
    }

    [Fact]
    public async Task AddHealthCheck_RejectsUnknownStatus()
    {
        var plant = await CreatePlant("Pickycard");

        var response = await Checkup(plant.Id, new { status = "Enlightened" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthCheck_RejectsMissingStatus()
    {
        var plant = await CreatePlant("Silent Simon");

        var response = await Checkup(plant.Id, new { note = "no status given" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthCheck_RejectsOverlongNote()
    {
        var plant = await CreatePlant("Verbose Victor");

        var response = await Checkup(plant.Id, new { status = "Good", note = new string('x', 501) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_OnUnknownPlant_Returns404()
    {
        var add = await Checkup(424242, new { status = "Good" });
        Assert.Equal(HttpStatusCode.NotFound, add.StatusCode);

        var list = await _client.GetAsync("/api/plants/424242/health-checks");
        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
    }

    [Fact]
    public async Task FertilizingTask_WhileSick_PausesWithHint()
    {
        var plant = await CreatePlant("Hungry Henry", intervalDays: 7, daysSinceWatered: 1);
        var added = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/care-tasks", new
        {
            type = "Fertilizing",
            intervalDays = 30,
            reduceInWinter = false,
        }, Options);
        added.EnsureSuccessStatusCode();

        await Checkup(plant.Id, new { status = "Sick" });

        var tasks = await _client.GetFromJsonAsync<List<CareTaskResponseDto>>($"/api/plants/{plant.Id}/care-tasks", Options);
        var fertilizing = tasks!.Single(t => t.Type == CareTaskType.Fertilizing);

        Assert.Contains("paused", fertilizing.Hint);
        Assert.Equal(PlantDueStatus.Upcoming, fertilizing.DueStatus);
    }

    [Fact]
    public async Task Dashboard_And_List_CarryHealthStatus()
    {
        var plant = await CreatePlant("Visible Vera");
        await Checkup(plant.Id, new { status = "Bad" });

        var list = await _client.GetFromJsonAsync<List<PlantResponseDto>>("/api/plants", Options);
        Assert.Equal(HealthStatus.Bad, list!.Single(p => p.Id == plant.Id).HealthStatus);

        var dashboard = await _client.GetFromJsonAsync<DashboardResponseDto>("/api/dashboard", Options);
        Assert.Contains(dashboard!.Upcoming, p => p.Id == plant.Id && p.HealthStatus == HealthStatus.Bad);
    }

    [Fact]
    public async Task Export_CarriesHealthStatusAndHistory()
    {
        var plant = await CreatePlant("Exported Elsa");
        await Checkup(plant.Id, new { status = "Excellent", note = "Growing like mad" });

        var document = await _client.GetFromJsonAsync<ExportDocumentDto>("/api/export", Options);

        var exported = document!.Plants.Single(p => p.NickName == "Exported Elsa");
        Assert.Equal(HealthStatus.Excellent, exported.HealthStatus);
        Assert.NotNull(exported.LastCheckupAt);
        var check = Assert.Single(exported.HealthChecks);
        Assert.Equal("Growing like mad", check.Note);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
