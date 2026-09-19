using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/plants")]
[Produces("application/json")]
public class PlantsController(IPlantService plants) : ControllerBase
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
    public async Task<ActionResult<PlantResponseDto>> Create(CreatePlantRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await plants.CreateAsync(dto, cancellationToken);
        return result.Status switch
        {
            PlantWriteStatus.InvalidProfile => BadRequest(new ProblemDetails { Title = "Unknown plant profile.", Detail = $"No plant profile with id {dto.PlantProfileId} exists." }),
            _ => CreatedAtAction(nameof(Get), new { id = result.Plant!.Id }, result.Plant),
        };
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlantResponseDto>> Update(int id, UpdatePlantRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await plants.UpdateAsync(id, dto, cancellationToken);
        return result.Status switch
        {
            PlantWriteStatus.NotFound => NotFound(),
            PlantWriteStatus.InvalidProfile => BadRequest(new ProblemDetails { Title = "Unknown plant profile.", Detail = $"No plant profile with id {dto.PlantProfileId} exists." }),
            _ => Ok(result.Plant),
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => await plants.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:int}/water")]
    public async Task<ActionResult<PlantResponseDto>> Water(int id, WaterPlantRequestDto? dto, CancellationToken cancellationToken)
    {
        var plant = await plants.WaterAsync(id, dto?.Note, cancellationToken);
        return plant is null ? NotFound() : Ok(plant);
    }

    [HttpDelete("{id:int}/water")]
    public async Task<ActionResult<PlantResponseDto>> UndoWater(int id, CancellationToken cancellationToken)
    {
        var plant = await plants.UndoWaterAsync(id, cancellationToken);
        return plant is null ? NotFound() : Ok(plant);
    }

    [HttpPost("{id:int}/photo")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<PlantResponseDto>> UploadPhoto(int id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "No photo supplied.", Detail = "Send the image in the multipart form field \"file\"." });
        }

        await using var stream = file.OpenReadStream();
        var result = await plants.UploadPhotoAsync(id, stream, file.ContentType, cancellationToken);

        return result.Status switch
        {
            PlantPhotoStatus.NotFound => NotFound(),
            PlantPhotoStatus.InvalidFile => BadRequest(new ProblemDetails { Title = "Unsupported photo.", Detail = result.Error }),
            _ => Ok(result.Plant),
        };
    }

    [HttpGet("{id:int}/watering-logs")]
    public async Task<ActionResult<IReadOnlyList<WateringLogResponseDto>>> WateringLogs(int id, CancellationToken cancellationToken)
    {
        var logs = await plants.GetWateringHistoryAsync(id, cancellationToken);
        return logs is null ? NotFound() : Ok(logs);
    }
}
