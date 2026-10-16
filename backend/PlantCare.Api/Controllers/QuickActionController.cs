using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

/// <summary>
/// Shared-secret trigger link (from ntfy notification buttons) that waters a
/// plant with a single tap. Disabled (404) unless QUICK_ACTION_SECRET is set.
/// Attempts are rate limited per plant+IP and every result is logged.
/// </summary>
[ApiController]
[Route("api/plants/{id:int}/quick-water")]
public class QuickActionController(
    IPlantService plants,
    QuickActionOptions options,
    QuickActionRateLimiter limiter,
    IHttpContextAccessor httpContext,
    IAppLocalizer localizer,
    ILogger<QuickActionController> logger) : ControllerBase
{
    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Water(int id, [FromQuery] string? key, CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            return NotFound();
        }

        var remoteIp = httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // The secret is validated first so a correct key never spends a rate-limit
        // slot; only rejected attempts are counted (that is what blunts brute force).
        if (!QuickActionRateLimiter.SecretMatches(key, options.Secret!))
        {
            if (!limiter.TryAllow($"{id}:{remoteIp}"))
            {
                logger.LogWarning("Quick action for plant {PlantId} rate limited ({RemoteIp}).", id, remoteIp);
                return StatusCode(StatusCodes.Status429TooManyRequests, localizer.T("quick.rateLimited"));
            }

            logger.LogWarning("Quick action for plant {PlantId} rejected: bad secret ({RemoteIp}).", id, remoteIp);
            return StatusCode(StatusCodes.Status403Forbidden, localizer.T("quick.invalidKey"));
        }

        var plant = await plants.WaterAsync(id, localizer.T("quick.note"), cancellationToken: cancellationToken);
        if (plant is null)
        {
            logger.LogWarning("Quick action hit unknown plant {PlantId} ({RemoteIp}).", id, remoteIp);
            return NotFound(localizer.T("quick.notFound"));
        }

        logger.LogInformation("Quick action watered plant {PlantId} ({RemoteIp}).", id, remoteIp);
        return Ok(localizer.Tf("quick.done", plant.NickName));
    }
}
