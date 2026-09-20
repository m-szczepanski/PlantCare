using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/plant-profiles")]
[Produces("application/json")]
public class PlantProfilesController(IPlantProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlantProfileResponseDto>>> List(CancellationToken cancellationToken)
        => Ok(await profiles.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PlantProfileResponseDto>> Create(PlantProfileRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await profiles.CreateAsync(dto, cancellationToken);
        return result.Status switch
        {
            PlantProfileWriteStatus.DuplicateName => Conflict(new ProblemDetails { Title = "Profile name already in use.", Detail = $"A plant profile named '{dto.CommonName}' already exists." }),
            PlantProfileWriteStatus.InvalidChecklist => BadRequest(new ProblemDetails { Title = "Invalid diagnosis checklist.", Detail = "Provide a JSON array of { symptom, causes[] } entries with non-empty values." }),
            _ => StatusCode(StatusCodes.Status201Created, result.Profile),
        };
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlantProfileResponseDto>> Update(int id, PlantProfileRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await profiles.UpdateAsync(id, dto, cancellationToken);
        return result.Status switch
        {
            PlantProfileWriteStatus.NotFound => NotFound(),
            PlantProfileWriteStatus.DuplicateName => Conflict(new ProblemDetails { Title = "Profile name already in use.", Detail = $"A plant profile named '{dto.CommonName}' already exists." }),
            PlantProfileWriteStatus.InvalidChecklist => BadRequest(new ProblemDetails { Title = "Invalid diagnosis checklist.", Detail = "Provide a JSON array of { symptom, causes[] } entries with non-empty values." }),
            _ => Ok(result.Profile),
        };
    }
}
