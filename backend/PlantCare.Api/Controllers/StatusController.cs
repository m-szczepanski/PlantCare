using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/status")]
[Produces("application/json")]
public class StatusController(IStatusService status) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StatusInfo>> Get(CancellationToken cancellationToken)
        => Ok(await status.GetAsync(cancellationToken));
}
