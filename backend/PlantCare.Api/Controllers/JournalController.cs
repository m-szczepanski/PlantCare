using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/plants/{plantId:int}/journal")]
[Produces("application/json")]
public class JournalController(IJournalService journal) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JournalEntryResponseDto>>> List(int plantId, CancellationToken cancellationToken)
    {
        var entries = await journal.ListAsync(plantId, cancellationToken);
        return entries is null ? NotFound() : Ok(entries);
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<JournalEntryResponseDto>> Create(
        int plantId,
        [FromForm] DateTime? entryDate,
        [FromForm] string? text,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        await using var stream = file?.OpenReadStream();
        var result = await journal.CreateAsync(
            plantId,
            entryDate,
            text,
            file is null ? null : stream,
            file?.ContentType,
            cancellationToken);

        return result.Status switch
        {
            JournalWriteStatus.PlantNotFound => NotFound(),
            JournalWriteStatus.Empty => BadRequest(new ProblemDetails { Title = "A journal entry needs a photo, a note, or both." }),
            _ => Created($"/api/plants/{plantId}/journal", result.Entry),
        };
    }

    [HttpDelete("{entryId:int}")]
    public async Task<IActionResult> Delete(int plantId, int entryId, CancellationToken cancellationToken)
        => await journal.DeleteAsync(plantId, entryId, cancellationToken) ? NoContent() : NotFound();
}
