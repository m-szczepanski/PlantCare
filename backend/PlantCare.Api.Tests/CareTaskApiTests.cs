using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using Xunit;

namespace PlantCare.Api.Tests;

public class CareTaskApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public CareTaskApiTests()
    {
        var factory = _database.CreateFactory();
        _client = factory.CreateClient();
    }

    private async Task<PlantResponseDto> CreatePlant(int? interval = 7)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Tasked Terry",
            location = "Kitchen",
            customWateringIntervalDays = interval,
        }, Options);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!;
    }

    [Fact]
    public async Task CareTasks_ListedForPlant_WithDueInfo()
    {
        var plant = await CreatePlant();

        var tasks = await _client.GetFromJsonAsync<List<CareTaskResponseDto>>($"/api/plants/{plant.Id}/care-tasks", Options);

        Assert.NotNull(tasks);
        var watering = Assert.Single(tasks);
        Assert.Equal("Watering", watering.Type.ToString());
        Assert.Equal(7, watering.IntervalDays);
        Assert.Equal(7, watering.DaysUntilDue);
    }

    [Fact]
    public async Task MarkDone_Watering_ShowsUpInLegacyEndpoints()
    {
        var plant = await CreatePlant();

        var response = await _client.PostAsJsonAsync(
            $"/api/plants/{plant.Id}/care-tasks/watering/done",
            new { note = "Carefully" },
            Options);
        response.EnsureSuccessStatusCode();

        var task = await response.Content.ReadFromJsonAsync<CareTaskResponseDto>(Options);
        Assert.NotNull(task!.LastDoneAt);
        Assert.Equal(PlantDueStatus.Upcoming, task.DueStatus);

        var fetched = await _client.GetFromJsonAsync<PlantResponseDto>($"/api/plants/{plant.Id}", Options);
        Assert.Equal(task.LastDoneAt, fetched!.LastWateredAt);

        var logs = await _client.GetFromJsonAsync<List<WateringLogResponseDto>>($"/api/plants/{plant.Id}/watering-logs", Options);
        Assert.Equal("Carefully", Assert.Single(logs!).Note);

    }

    [Fact]
    public async Task MarkDone_LegacyWaterEndpoint_StillUpdatesTask()
    {
        var plant = await CreatePlant();

        var response = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/water", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tasks = await _client.GetFromJsonAsync<List<CareTaskResponseDto>>($"/api/plants/{plant.Id}/care-tasks", Options);
        Assert.NotNull(tasks!.Single().LastDoneAt);
    }

    [Fact]
    public async Task MarkDone_UnknownType_Returns400()
    {
        var plant = await CreatePlant();

        var response = await _client.PostAsync($"/api/plants/{plant.Id}/care-tasks/mowing/done", new StringContent(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReduceInWinter_RoundTripsThroughPlantEndpoints()
    {
        var plant = await CreatePlant();

        var response = await _client.PutAsJsonAsync($"/api/plants/{plant.Id}", new
        {
            nickName = plant.NickName,
            customWateringIntervalDays = 7,
            reduceInWinter = true,
        }, Options);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        Assert.Equal(true, updated!.ReduceInWinter);

        var tasks = await _client.GetFromJsonAsync<List<CareTaskResponseDto>>($"/api/plants/{plant.Id}/care-tasks", Options);
        Assert.Equal(true, tasks!.Single().ReduceInWinter);
    }

    [Fact]
    public async Task CareTasks_UnknownPlant_Returns404()
    {
        var response = await _client.GetAsync("/api/plants/424242/care-tasks");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Fertilizing_AddListMarkDoneAndRemove()
    {
        var plant = await CreatePlant();

        var created = await _client.PostAsJsonAsync(
            $"/api/plants/{plant.Id}/care-tasks",
            new { type = "Fertilizing", intervalDays = 30, reduceInWinter = true },
            Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var task = await created.Content.ReadFromJsonAsync<CareTaskResponseDto>(Options);

        var tasks = await _client.GetFromJsonAsync<List<CareTaskResponseDto>>($"/api/plants/{plant.Id}/care-tasks", Options);
        Assert.Equal(2, tasks!.Count);
        Assert.Contains("Fertilizing", tasks.Select(t => t.Type.ToString()));

        var done = await _client.PostAsJsonAsync($"/api/plants/{plant.Id}/care-tasks/fertilizing/done", new { }, Options);
        done.EnsureSuccessStatusCode();
        var doneTask = await done.Content.ReadFromJsonAsync<CareTaskResponseDto>(Options);
        Assert.NotNull(doneTask!.LastDoneAt);

        var duplicate = await _client.PostAsJsonAsync(
            $"/api/plants/{plant.Id}/care-tasks",
            new { type = "Fertilizing", intervalDays = 14 },
            Options);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var delete = await _client.DeleteAsync($"/api/plants/{plant.Id}/care-tasks/{task!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var remaining = await _client.GetFromJsonAsync<List<CareTaskResponseDto>>($"/api/plants/{plant.Id}/care-tasks", Options);
        Assert.Equal("Watering", Assert.Single(remaining!).Type.ToString());
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}
