using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using Xunit;

namespace PlantCare.Api.Tests;

public class ExportImportApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _source = new();
    private readonly TempDatabase _target = new();

    private async Task<ExportDocumentDto> ExportSourceAsync()
    {
        using var factory = _source.CreateFactory();
        var client = factory.CreateClient();

        var room = await client.PostAsJsonAsync("/api/rooms", new { name = "Attic", orientation = "North" }, Options);
        var roomId = (await room.Content.ReadFromJsonAsync<RoomResponseDto>(Options))!.Id;
        var plant = await client.PostAsJsonAsync("/api/plants", new
        {
            nickName = "Exported Ernie",
            roomId,
            customWateringIntervalDays = 7,
            lastWateredAt = DateTime.UtcNow.Date.AddDays(-3),
        }, Options);
        var plantId = (await plant.Content.ReadFromJsonAsync<PlantResponseDto>(Options))!.Id;
        await client.PostAsJsonAsync($"/api/plants/{plantId}/water", new { note = "twice" }, Options);
        var note = await client.PostAsJsonAsync(
            $"/api/plants/{plantId}/notes", new { text = "looks happy" }, Options);
        var noteId = (await note.Content.ReadFromJsonAsync<PlantNoteResponseDto>(Options))!.Id;
        await client.PutAsJsonAsync(
            $"/api/plants/{plantId}/notes/{noteId}/pin", new { pinned = true }, Options);

        var exportResponse = await client.GetAsync("/api/export");
        exportResponse.EnsureSuccessStatusCode();
        return (await exportResponse.Content.ReadFromJsonAsync<ExportDocumentDto>(Options))!;
    }

    [Fact]
    public async Task ExportThenImport_RecreatesGraph_AndSecondImportSkips()
    {
        var document = await ExportSourceAsync();

        Assert.Equal(1, document.SchemaVersion);
        Assert.Contains(document.Rooms, r => r.Name == "Attic");
        var ernie = document.Plants.Single(p => p.NickName == "Exported Ernie");
        Assert.Equal("Attic", ernie.RoomName);
        Assert.NotEmpty(ernie.CareTasks.Single(t => t.Type.ToString() == "Watering").Logs);
        Assert.Single(ernie.Notes);
        Assert.True(ernie.Notes[0].Pinned);

        using var targetFactory = _target.CreateFactory();
        var target = targetFactory.CreateClient();

        var imported = await target.PostAsJsonAsync("/api/import", document, Options);
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        var first = await imported.Content.ReadFromJsonAsync<ImportResultDto>(Options);
        Assert.True(first!.PlantsCreated >= 1);

        var targetPlants = await target.GetFromJsonAsync<List<PlantResponseDto>>("/api/plants", Options);
        var restored = targetPlants!.Single(p => p.NickName == "Exported Ernie");
        Assert.Equal("Attic", restored.RoomName);
        var restoredNotes = await target.GetFromJsonAsync<List<PlantNoteResponseDto>>(
            $"/api/plants/{restored.Id}/notes",
            Options);
        Assert.True(Assert.Single(restoredNotes!).IsPinned);
        var logs = await target.GetFromJsonAsync<List<WateringLogResponseDto>>(
            $"/api/plants/{restored.Id}/watering-logs",
            Options);
        Assert.Single(logs!);

        var secondResponse = await target.PostAsJsonAsync("/api/import", document, Options);
        var second = await secondResponse.Content.ReadFromJsonAsync<ImportResultDto>(Options);
        Assert.Equal(0, second!.PlantsCreated);
        Assert.Equal(1, second.PlantsSkipped);
    }

    [Fact]
    public async Task Import_UnsupportedSchema_Returns400()
    {
        using var factory = _target.CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/import", new
        {
            schemaVersion = 99,
            exportedAt = DateTime.UtcNow,
            rooms = Array.Empty<object>(),
            plantProfiles = Array.Empty<object>(),
            plants = Array.Empty<object>(),
        }, Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_Conflict_RollsBackAndReturns400()
    {
        using var factory = _target.CreateFactory();
        var client = factory.CreateClient();

        // Two care tasks of the same type for one plant violate the (PlantId, Type)
        // unique index; the whole import must roll back (no room left behind).
        var document = new ExportDocumentDto
        {
            SchemaVersion = 1,
            ExportedAt = DateTime.UtcNow,
            Rooms = [new ExportRoomDto { Name = "Rollback Room" }],
            PlantProfiles = [],
            Plants =
            [
                new ExportPlantDto
                {
                    NickName = "Duplicate tasks",
                    RoomName = "Rollback Room",
                    AcquiredDate = DateTime.UtcNow.Date,
                    CareTasks =
                    [
                        new ExportCareTaskDto { Type = CareTaskType.Watering, IntervalDays = 7, Logs = [] },
                        new ExportCareTaskDto { Type = CareTaskType.Watering, IntervalDays = 7, Logs = [] },
                    ],
                    Notes = [],
                    Journal = [],
                },
            ],
        };

        var response = await client.PostAsJsonAsync("/api/import", document, Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var rooms = await client.GetFromJsonAsync<List<RoomResponseDto>>("/api/rooms", Options);
        Assert.DoesNotContain(rooms!, r => r.Name == "Rollback Room");
    }

    public void Dispose()
    {
        _source.Dispose();
        _target.Dispose();
        GC.SuppressFinalize(this);
    }
}
