using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Produces("application/json")]
public class DevicesController(IDeviceService devices, FeatureFlags features) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeviceResponseDto>>> List(CancellationToken cancellationToken)
    {
        if (!features.DevicesEnabled)
        {
            return NotFound();
        }

        return Ok(await devices.ListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeviceResponseDto>> Get(int id, CancellationToken cancellationToken)
    {
        if (!features.DevicesEnabled)
        {
            return NotFound();
        }

        var all = await devices.ListAsync(cancellationToken);
        var device = all.FirstOrDefault(d => d.Id == id);
        return device is null ? NotFound() : Ok(device);
    }

    [HttpPost]
    public async Task<ActionResult<DeviceResponseDto>> Create(DeviceRequestDto dto, CancellationToken cancellationToken)
    {
        if (!features.DevicesEnabled)
        {
            return NotFound();
        }

        var result = await devices.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Device!.Id }, result.Device);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DeviceResponseDto>> Update(int id, DeviceRequestDto dto, CancellationToken cancellationToken)
    {
        if (!features.DevicesEnabled)
        {
            return NotFound();
        }

        var result = await devices.UpdateAsync(id, dto, cancellationToken);
        return result.Status switch
        {
            DeviceWriteStatus.NotFound => NotFound(),
            _ => Ok(result.Device),
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!features.DevicesEnabled)
        {
            return NotFound();
        }

        return await devices.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}
