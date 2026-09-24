using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

/// <summary>
/// On-demand notification controls for verifying delivery without waiting for the
/// scheduled digest: send a test push through every configured channel, or run the
/// watering check right now. Both are read-mostly aids for setup/troubleshooting.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Produces("application/json")]
public class NotificationsController(
    INotificationTestService testService,
    IWateringCheckService checkService) : ControllerBase
{
    /// <summary>Sends a test notification to every configured channel and reports per-channel results.</summary>
    [HttpPost("test")]
    public async Task<ActionResult<NotificationTestResult>> Test(
        [FromBody] TestNotificationRequestDto? request,
        CancellationToken cancellationToken)
        => Ok(await testService.SendTestAsync(request?.Message, cancellationToken));

    /// <summary>Runs the daily watering check immediately (honors the once-per-day digest dedup).</summary>
    [HttpPost("run-check")]
    public async Task<ActionResult<WateringCheckResult>> RunCheck(CancellationToken cancellationToken)
        => Ok(await checkService.RunAsync(cancellationToken));
}
