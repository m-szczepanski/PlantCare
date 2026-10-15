using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public enum DeviceWriteStatus
{
    Success,
    NotFound,
}

public sealed record DeviceWriteResult(DeviceWriteStatus Status, DeviceResponseDto? Device = null);

public interface IDeviceService
{
    Task<IReadOnlyList<DeviceResponseDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<DeviceWriteResult> CreateAsync(DeviceRequestDto dto, CancellationToken cancellationToken = default);

    Task<DeviceWriteResult> UpdateAsync(int id, DeviceRequestDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class DeviceService(AppDbContext db) : IDeviceService
{
    public async Task<IReadOnlyList<DeviceResponseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var devices = await db.Devices
            .Include(d => d.Plants)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return devices.Select(ToResponse).ToList();
    }

    public async Task<DeviceWriteResult> CreateAsync(DeviceRequestDto dto, CancellationToken cancellationToken = default)
    {
        var device = new Device
        {
            Name = dto.Name.Trim(),
            Type = dto.Type,
            CommandStyle = dto.CommandStyle,
            CommandTopic = Normalize(dto.CommandTopic),
            StateTopic = Normalize(dto.StateTopic),
        };
        db.Devices.Add(device);
        await db.SaveChangesAsync(cancellationToken);

        return new DeviceWriteResult(DeviceWriteStatus.Success, await ReloadAsync(device.Id, cancellationToken));
    }

    public async Task<DeviceWriteResult> UpdateAsync(int id, DeviceRequestDto dto, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (device is null)
        {
            return new DeviceWriteResult(DeviceWriteStatus.NotFound);
        }

        device.Name = dto.Name.Trim();
        device.Type = dto.Type;
        device.CommandStyle = dto.CommandStyle;
        device.CommandTopic = Normalize(dto.CommandTopic);
        device.StateTopic = Normalize(dto.StateTopic);
        await db.SaveChangesAsync(cancellationToken);

        return new DeviceWriteResult(DeviceWriteStatus.Success, await ReloadAsync(device.Id, cancellationToken));
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (device is null)
        {
            return false;
        }

        db.Devices.Remove(device);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<DeviceResponseDto?> ReloadAsync(int id, CancellationToken cancellationToken)
    {
        var device = await db.Devices
            .Include(d => d.Plants)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return device is null ? null : ToResponse(device);
    }

    private static string? Normalize(string? topic)
        => string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();

    private static DeviceResponseDto ToResponse(Device device) => new()
    {
        Id = device.Id,
        Name = device.Name,
        Type = device.Type,
        CommandStyle = device.CommandStyle,
        CommandTopic = device.CommandTopic,
        StateTopic = device.StateTopic,
        LastMoisturePct = device.LastMoisturePct,
        WaterLevel = device.WaterLevel,
        LastSeenAt = device.LastSeenAt,
        PlantCount = device.Plants.Count,
    };
}
