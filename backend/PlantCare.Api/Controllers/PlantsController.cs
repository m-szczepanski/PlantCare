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
}
