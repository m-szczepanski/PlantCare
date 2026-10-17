using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api")]
public partial class ExportImportController(IExportImportService exportImport, IAppLocalizer localizer) : ControllerBase
{
    private static readonly JsonSerializerOptions ExportOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
    };

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var document = await exportImport.ExportAsync(cancellationToken);
        return new FileContentResult(
            JsonSerializer.SerializeToUtf8Bytes(document, ExportOptions),
            "application/json")
        {
            FileDownloadName = $"plantcare-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.json",
        };
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportResultDto>> Import(
        ExportDocumentDto document,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await exportImport.ImportAsync(document, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new ProblemDetails { Title = localizer.T("error.importFailed.title"), Detail = ex.Message });
        }
        catch (DbUpdateException)
        {
            // A hand-edited document can violate a unique index (duplicate care
            // task type, name race). The import transaction rolls back, so report
            // it as a client error rather than an unhandled 500.
            return BadRequest(new ProblemDetails
            {
                Title = localizer.T("error.importFailed.title"),
                Detail = localizer.T("error.importFailed.detail"),
            });
        }
    }
}
