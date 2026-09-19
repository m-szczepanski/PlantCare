using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
public class CalendarController(ICalendarService calendar) : ControllerBase
{
    [HttpGet("api/calendar.ics")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var feed = await calendar.BuildFeedAsync(cancellationToken);
        return Content(feed, "text/calendar; charset=utf-8");
    }
}
