using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/plants")]
[Produces("application/json")]
public class PlantsController(IPlantService plants, ICareTaskService careTasks, IHealthCheckService healthChecks,
    IAppLocalizer localizer, IDeviceCommandService deviceCommands, FeatureFlags features) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlantResponseDto>>> List(CancellationToken cancellationToken)
        => Ok(await plants.ListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlantResponseDto>> Get(int id, CancellationToken cancellationToken)
    {
        var plant = await plants.GetAsync(id, cancellationToken);
        return plant is null ? NotFound() : Ok(plant);
    }

    [HttpPost]
    public async Task<ActionResult<PlantResponseDto>> Create(CreatePlantRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await plants.CreateAsync(dto, cancellationToken);
        return result.Status switch
        {
            PlantWriteStatus.InvalidProfile => BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.unknownProfile.title"),
                Detail = localizer.Tf("error.unknownProfile.detail", dto.PlantProfileId!)
            }),
            PlantWriteStatus.InvalidRoom => BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.unknownRoom.title"),
                Detail = localizer.Tf("error.unknownRoom.detail", dto.RoomId!)
            }),
            PlantWriteStatus.InvalidDevice => BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.unknownDevice.title"),
                Detail = localizer.Tf("error.unknownDevice.detail", dto.DeviceId!)
            }),
            _ => CreatedAtAction(nameof(Get), new { id = result.Plant!.Id }, result.Plant),
        };
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlantResponseDto>> Update(int id, UpdatePlantRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await plants.UpdateAsync(id, dto, cancellationToken);
        return result.Status switch
        {
            PlantWriteStatus.NotFound => NotFound(),
            PlantWriteStatus.InvalidProfile => BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.unknownProfile.title"),
                Detail = localizer.Tf("error.unknownProfile.detail", dto.PlantProfileId!)
            }),
            PlantWriteStatus.InvalidRoom => BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.unknownRoom.title"),
                Detail = localizer.Tf("error.unknownRoom.detail", dto.RoomId!)
            }),
            PlantWriteStatus.InvalidDevice => BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.unknownDevice.title"),
                Detail = localizer.Tf("error.unknownDevice.detail", dto.DeviceId!)
            }),
            _ => Ok(result.Plant),
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => await plants.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:int}/water")]
    public async Task<ActionResult<PlantResponseDto>> Water(int id, WaterPlantRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var plant = await plants.WaterAsync(id, dto?.Note, dto?.AmountMilliliters, dto?.Method, cancellationToken);
        return plant is null ? NotFound() : Ok(plant);
    }

    [HttpPost("bulk-water")]
    public async Task<ActionResult<BulkWaterResponseDto>> BulkWater(BulkWaterRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await plants.BulkWaterAsync(dto.Ids, cancellationToken);
        return Ok(new BulkWaterResponseDto
        {
            Requested = result.Requested,
            Watered = result.Watered,
            SkippedIds = result.SkippedIds,
        });
    }

    [HttpDelete("{id:int}/water")]
    public async Task<ActionResult<PlantResponseDto>> UndoWater(int id, CancellationToken cancellationToken)
    {
        var plant = await plants.UndoWaterAsync(id, cancellationToken);
        return plant is null ? NotFound() : Ok(plant);
    }

    [HttpPost("{id:int}/device-water")]
    public async Task<ActionResult> DeviceWater(int id, DeviceWaterRequestDto? dto, CancellationToken cancellationToken)
    {
        if (!features.DevicesEnabled)
        {
            return NotFound();
        }
        var result = await deviceCommands.WaterAsync(id, dto?.DurationMs, cancellationToken);
        return result.Status switch
        {
            DeviceWaterStatus.PlantNotFound => NotFound(),
            DeviceWaterStatus.NoDevice => BadRequest(new ProblemDetails
            {
                Title = localizer.T(
                    "error.noDevice.title"),
                Detail = localizer.T("error.noDevice.detail")
            }),
            DeviceWaterStatus.PublishFailed => Problem(statusCode: 502, title: localizer.T(
                "error.deviceOffline.title"), detail: localizer.T("error.deviceOffline.detail")),
            _ => Accepted(),
        };

    }

    [HttpPost("{id:int}/photo")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<PlantResponseDto>> UploadPhoto(int id, IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.noPhoto.title"),
                Detail = localizer.T("error.noPhoto.detail")
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await plants.UploadPhotoAsync(id, stream, file.ContentType, cancellationToken);

        return result.Status switch
        {
            PlantPhotoStatus.NotFound => NotFound(),
            PlantPhotoStatus.InvalidFile => BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.unsupportedPhoto.title"),
                Detail = result.ErrorKey is null ? null : localizer.Tf(result.ErrorKey, result.ErrorArgs ?? [])
            }),
            _ => Ok(result.Plant),
        };
    }

    [HttpPost("{id:int}/snooze")]
    public async Task<ActionResult<PlantResponseDto>> Snooze(int id, SnoozeRequestDto dto,
        CancellationToken cancellationToken)
    {
        var plant = await plants.SetSnoozeAsync(id, dto.Days, cancellationToken);
        return plant is null ? NotFound() : Ok(plant);
    }

    [HttpDelete("{id:int}/snooze")]
    public async Task<ActionResult<PlantResponseDto>> ClearSnooze(int id, CancellationToken cancellationToken)
    {
        var plant = await plants.ClearSnoozeAsync(id, cancellationToken);
        return plant is null ? NotFound() : Ok(plant);
    }

    [HttpPost("snooze-all")]
    public async Task<ActionResult<SnoozeAllResponseDto>> SnoozeAll(
        SnoozeRequestDto dto,
        CancellationToken cancellationToken)
    {
        var count = await plants.SnoozeAllAsync(dto.Days, cancellationToken);
        return Ok(new SnoozeAllResponseDto { SnoozedPlants = count });
    }

    [HttpPost("{id:int}/soil-wet")]
    public async Task<ActionResult<PlantResponseDto>> SetSoilWet(int id, SoilWetRequestDto dto,
        CancellationToken cancellationToken)
    {
        var plant = await plants.SetSoilWetAsync(id, dto.Days, cancellationToken);
        return plant is null ? NotFound() : Ok(plant);
    }

    [HttpDelete("{id:int}/soil-wet")]
    public async Task<ActionResult<PlantResponseDto>> ClearSoilWet(int id, CancellationToken cancellationToken)
    {
        var plant = await plants.ClearSoilWetAsync(id, cancellationToken);
        return plant is null ? NotFound() : Ok(plant);
    }

    [HttpGet("{id:int}/care-tasks")]
    public async Task<ActionResult<IReadOnlyList<CareTaskResponseDto>>> CareTasks(int id,
        CancellationToken cancellationToken)
    {
        var tasks = await careTasks.ListForPlantAsync(id, cancellationToken);
        return tasks is null ? NotFound() : Ok(tasks);
    }

    [HttpPost("{id:int}/care-tasks")]
    public async Task<ActionResult<CareTaskResponseDto>> CreateCareTask(int id, CreateCareTaskRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await careTasks.CreateAsync(id, dto.Type, dto.IntervalDays, dto.ReduceInWinter, cancellationToken);
        return result.Status switch
        {
            CareTaskWriteStatus.NotFound => NotFound(),
            CareTaskWriteStatus.AlreadyExists => Conflict(new ProblemDetails
            {
                Title = localizer.T(
                "error.duplicateCareTask.title")
            }),
            _ => Created($"/api/plants/{id}/care-tasks", result.Task),
        };
    }

    [HttpDelete("{id:int}/care-tasks/{taskId:int}")]
    public async Task<IActionResult> DeleteCareTask(int id, int taskId, CancellationToken cancellationToken)
        => await careTasks.DeleteAsync(id, taskId, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:int}/care-tasks/{type}/done")]
    public async Task<ActionResult<CareTaskResponseDto>> MarkCareTaskDone(int id, string type,
        WaterPlantRequestDto? dto, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CareTaskType>(type, ignoreCase: true, out var taskType))
        {
            return BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.unknownCareTaskType.title"),
                Detail = localizer.Tf("error.unknownCareTaskType.detail", type)
            });
        }
        var result = await careTasks.MarkDoneAsync(id, taskType, dto?.Note, dto?.AmountMilliliters,
            dto?.Method, cancellationToken);
        return result is null ? NotFound() : Ok(result);

    }

    [HttpGet("{id:int}/notes")]
    public async Task<ActionResult<IReadOnlyList<PlantNoteResponseDto>>> Notes(int id,
        CancellationToken cancellationToken)
    {
        var notes = await plants.GetNotesAsync(id, cancellationToken);
        return notes is null ? NotFound() : Ok(notes);
    }

    [HttpPost("{id:int}/notes")]
    public async Task<ActionResult<PlantNoteResponseDto>> AddNote(int id, PlantNoteRequestDto dto,
        CancellationToken cancellationToken)
    {
        var note = await plants.AddNoteAsync(id, dto.Text, cancellationToken);
        return note is null ? NotFound() : Created($"/api/plants/{id}/notes/{note.Id}", note);
    }

    [HttpPut("{id:int}/notes/{noteId:int}")]
    public async Task<ActionResult<PlantNoteResponseDto>> UpdateNote(int id, int noteId,
        PlantNoteRequestDto dto, CancellationToken cancellationToken)
    {
        var note = await plants.UpdateNoteAsync(id, noteId, dto.Text, cancellationToken);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpDelete("{id:int}/notes/{noteId:int}")]
    public async Task<IActionResult> DeleteNote(int id, int noteId, CancellationToken cancellationToken)
    {
        var deleted = await plants.DeleteNoteAsync(id, noteId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/notes/restore")]
    public async Task<ActionResult<PlantNoteResponseDto>> RestoreNote(int id,
        RestorePlantNoteRequestDto dto, CancellationToken cancellationToken)
    {
        var note = await plants.RestoreNoteAsync(id, dto.CreatedAt, dto.Text, dto.Pinned, cancellationToken);
        return note is null ? NotFound() : Created($"/api/plants/{id}/notes/{note.Id}", note);
    }

    [HttpPut("{id:int}/notes/{noteId:int}/pin")]
    public async Task<ActionResult<PlantNoteResponseDto>> PinNote(int id, int noteId,
        PinPlantNoteRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await plants.SetNotePinnedAsync(id, noteId, dto.Pinned, cancellationToken);
        return result.Status switch
        {
            PlantNotePinStatus.NotFound => NotFound(),
            PlantNotePinStatus.LimitReached => BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.tooManyPinnedNotes.title"),
                Detail = localizer.Tf("error.tooManyPinnedNotes.detail", PlantService.MaxPinnedNotes)
            }),
            _ => Ok(result.Note),
        };
    }

    [HttpGet("{id:int}/watering-logs")]
    public async Task<ActionResult<IReadOnlyList<WateringLogResponseDto>>> WateringLogs(int id,
        CancellationToken cancellationToken)
    {
        var logs = await plants.GetWateringHistoryAsync(id, cancellationToken);
        return logs is null ? NotFound() : Ok(logs);
    }

    [HttpGet("{id:int}/health-checks")]
    public async Task<ActionResult<IReadOnlyList<HealthCheckResponseDto>>> HealthChecks(int id,
        CancellationToken cancellationToken)
    {
        var checks = await healthChecks.ListAsync(id, cancellationToken);
        return checks is null ? NotFound() : Ok(checks);
    }

    [HttpPost("{id:int}/health-checks")]
    public async Task<ActionResult<PlantResponseDto>> AddHealthCheck(int id, CreateHealthCheckRequestDto dto,
        CancellationToken cancellationToken)
    {
        var plant = await healthChecks.AddAsync(id, dto.Status!.Value, dto.Note, cancellationToken);
        return plant is null ? NotFound() : Ok(plant);
    }
}
