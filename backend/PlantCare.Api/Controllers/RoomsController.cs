using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Dtos;
using PlantCare.Api.Services;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Produces("application/json")]
public class RoomsController(IRoomService rooms) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomResponseDto>>> List(CancellationToken cancellationToken)
        => Ok(await rooms.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<RoomResponseDto>> Create(RoomRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await rooms.CreateAsync(dto, cancellationToken);
        return result.Status switch
        {
            RoomWriteStatus.DuplicateName => Conflict(new ProblemDetails { Title = "A room with that name already exists." }),
            _ => CreatedAtAction(nameof(Get), new { id = result.Room!.Id }, result.Room),
        };
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponseDto>> Get(int id, CancellationToken cancellationToken)
    {
        var all = await rooms.ListAsync(cancellationToken);
        var room = all.FirstOrDefault(r => r.Id == id);
        return room is null ? NotFound() : Ok(room);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomResponseDto>> Update(int id, RoomRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await rooms.UpdateAsync(id, dto, cancellationToken);
        return result.Status switch
        {
            RoomWriteStatus.NotFound => NotFound(),
            RoomWriteStatus.DuplicateName => Conflict(new ProblemDetails { Title = "A room with that name already exists." }),
            _ => Ok(result.Room),
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => await rooms.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
