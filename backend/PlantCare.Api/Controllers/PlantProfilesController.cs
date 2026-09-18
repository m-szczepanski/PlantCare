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
}
