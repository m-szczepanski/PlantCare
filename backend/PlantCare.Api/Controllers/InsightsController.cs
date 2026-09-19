using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/insights")]
[Produces("application/json")]
public class InsightsController(IInsightsService insights) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<InsightsResponseDto>> Get(CancellationToken cancellationToken)
        => Ok(await insights.GetAsync(cancellationToken));
}
