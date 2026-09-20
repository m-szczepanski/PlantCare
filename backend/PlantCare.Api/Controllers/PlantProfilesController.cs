using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/plant-profiles")]
[Produces("application/json")]
public class PlantProfilesController(IPlantProfileService profiles, IAppLocalizer localizer) : ControllerBase
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
            PlantProfileWriteStatus.DuplicateName => Conflict(new ProblemDetails { Title = localizer.T("error.duplicateProfile.title"), Detail = localizer.Tf("error.duplicateProfile.detail", dto.CommonName) }),
            PlantProfileWriteStatus.InvalidChecklist => BadRequest(new ProblemDetails { Title = localizer.T("error.invalidChecklist.title"), Detail = localizer.T("error.invalidChecklist.detail") }),
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
            PlantProfileWriteStatus.DuplicateName => Conflict(new ProblemDetails { Title = localizer.T("error.duplicateProfile.title"), Detail = localizer.Tf("error.duplicateProfile.detail", dto.CommonName) }),
            PlantProfileWriteStatus.InvalidChecklist => BadRequest(new ProblemDetails { Title = localizer.T("error.invalidChecklist.title"), Detail = localizer.T("error.invalidChecklist.detail") }),
            _ => Ok(result.Profile),
        };
    }
}
