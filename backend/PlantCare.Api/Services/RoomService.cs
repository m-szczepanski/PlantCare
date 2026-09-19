using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public enum RoomWriteStatus
{
    Success,
    NotFound,
    DuplicateName,
}

public sealed record RoomWriteResult(RoomWriteStatus Status, RoomResponseDto? Room = null);

public interface IRoomService
{
    Task<IReadOnlyList<RoomResponseDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<RoomWriteResult> CreateAsync(RoomRequestDto dto, CancellationToken cancellationToken = default);

    Task<RoomWriteResult> UpdateAsync(int id, RoomRequestDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class RoomService(AppDbContext db) : IRoomService
{
    public async Task<IReadOnlyList<RoomResponseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await db.Rooms
            .Include(r => r.Plants)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rooms.Select(ToResponse).ToList();
    }

    public async Task<RoomWriteResult> CreateAsync(RoomRequestDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();
        if (await db.Rooms.AnyAsync(r => r.Name == name, cancellationToken))
        {
            return new RoomWriteResult(RoomWriteStatus.DuplicateName);
        }

        var room = new Room
        {
            Name = name,
            Orientation = dto.Orientation,
            LightExposure = dto.LightExposure,
            Humidity = dto.Humidity,
            TemperatureCelsius = dto.TemperatureCelsius,
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync(cancellationToken);

        return new RoomWriteResult(RoomWriteStatus.Success, await ReloadAsync(room.Id, cancellationToken));
    }

    public async Task<RoomWriteResult> UpdateAsync(int id, RoomRequestDto dto, CancellationToken cancellationToken = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room is null)
        {
            return new RoomWriteResult(RoomWriteStatus.NotFound);
        }

        var name = dto.Name.Trim();
        if (name != room.Name && await db.Rooms.AnyAsync(r => r.Name == name, cancellationToken))
        {
            return new RoomWriteResult(RoomWriteStatus.DuplicateName);
        }

        room.Name = name;
        room.Orientation = dto.Orientation;
        room.LightExposure = dto.LightExposure;
        room.Humidity = dto.Humidity;
        room.TemperatureCelsius = dto.TemperatureCelsius;
        await db.SaveChangesAsync(cancellationToken);

        return new RoomWriteResult(RoomWriteStatus.Success, await ReloadAsync(room.Id, cancellationToken));
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room is null)
        {
            return false;
        }

        db.Rooms.Remove(room);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<RoomResponseDto?> ReloadAsync(int id, CancellationToken cancellationToken)
    {
        var room = await db.Rooms
            .Include(r => r.Plants)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return room is null ? null : ToResponse(room);
    }

    private static RoomResponseDto ToResponse(Room room) => new()
    {
        Id = room.Id,
        Name = room.Name,
        Orientation = room.Orientation,
        LightExposure = room.LightExposure,
        Humidity = room.Humidity,
        TemperatureCelsius = room.TemperatureCelsius,
        PlantCount = room.Plants.Count,
    };
}
