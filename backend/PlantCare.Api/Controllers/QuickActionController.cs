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
        if (!limiter.TryAllow($"{id}:{remoteIp}"))
        {
            logger.LogWarning("Quick action for plant {PlantId} rate limited ({RemoteIp}).", id, remoteIp);
            return StatusCode(StatusCodes.Status429TooManyRequests, "Too many quick-action attempts.");
        }

        if (!QuickActionRateLimiter.SecretMatches(key, options.Secret!))
        {
            logger.LogWarning("Quick action for plant {PlantId} rejected: bad secret ({RemoteIp}).", id, remoteIp);
            return StatusCode(StatusCodes.Status403Forbidden, "Invalid quick-action key.");
        }

        var plant = await plants.WaterAsync(id, "Watered via notification", cancellationToken: cancellationToken);
        if (plant is null)
        {
            logger.LogWarning("Quick action hit unknown plant {PlantId} ({RemoteIp}).", id, remoteIp);
            return NotFound("Plant not found.");
        }

        logger.LogInformation("Quick action watered plant {PlantId} ({RemoteIp}).", id, remoteIp);
        return Ok($"{plant.NickName} watered just now.");
    }
}
