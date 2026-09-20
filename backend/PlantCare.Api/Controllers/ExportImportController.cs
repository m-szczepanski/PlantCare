using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
public class ExportImportController(IExportImportService exportImport, IAppLocalizer localizer) : ControllerBase
{
    [HttpGet("api/export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var document = await exportImport.ExportAsync(cancellationToken);
        return new FileContentResult(
            JsonSerializer.SerializeToUtf8Bytes(document,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new JsonStringEnumConverter() },
                    WriteIndented = true,
                }),
            "application/json")
        {
            FileDownloadName = $"plantcare-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.json",
        };
    }

    [HttpPost("api/import")]
    public async Task<ActionResult<ImportResultDto>> Import(ExportDocumentDto document, CancellationToken cancellationToken)
    {
        try
        {
            var result = await exportImport.ImportAsync(document, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Title = localizer.T("error.importFailed.title"), Detail = ex.Message });
        }
    }
}
