using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/reference-data")]
[Produces("application/json")]
public class ReferenceDataController : ControllerBase
{
    [HttpGet("soil-types")]
    public ActionResult<IReadOnlyList<SoilTypeOptionDto>> GetSoilTypes()
        => Ok(SoilTypes.All
            .Select(s => new SoilTypeOptionDto { Type = s.Type, WateringIntervalFactor = s.WateringIntervalFactor, Mixes = s.Mixes })
            .ToList());
}
