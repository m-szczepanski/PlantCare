using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public enum PlantWriteStatus
{
    Success,
    NotFound,
    InvalidProfile,
}

public sealed record PlantWriteResult(PlantWriteStatus Status, PlantResponseDto? Plant = null);

public interface IPlantService
{
    Task<IReadOnlyList<PlantResponseDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<PlantResponseDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<PlantWriteResult> CreateAsync(CreatePlantRequestDto dto, CancellationToken cancellationToken = default);

    Task<PlantWriteResult> UpdateAsync(int id, UpdatePlantRequestDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class PlantService(AppDbContext db, IWateringScheduleService schedule) : IPlantService
{
    public async Task<IReadOnlyList<PlantResponseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var plants = await db.Plants
            .Include(p => p.PlantProfile)
            .OrderBy(p => p.NickName)
            .ToListAsync(cancellationToken);

        return plants.Select(ToResponse).ToList();
    }

    public async Task<PlantResponseDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants
            .Include(p => p.PlantProfile)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return plant is null ? null : ToResponse(plant);
    }

    public async Task<PlantWriteResult> CreateAsync(CreatePlantRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.PlantProfileId is not null && !await ProfileExistsAsync(dto.PlantProfileId.Value, cancellationToken))
        {
            return new PlantWriteResult(PlantWriteStatus.InvalidProfile);
        }

        var plant = new Plant
        {
            NickName = dto.NickName.Trim(),
            Location = dto.Location.Trim(),
            PhotoUrl = dto.PhotoUrl,
            AcquiredDate = dto.AcquiredDate,
            CustomWateringIntervalDays = dto.CustomWateringIntervalDays,
            PlantProfileId = dto.PlantProfileId,
            LastWateredAt = dto.LastWateredAt,
        };

        db.Plants.Add(plant);
        await db.SaveChangesAsync(cancellationToken);

        return new PlantWriteResult(PlantWriteStatus.Success, await ReloadAsync(plant.Id, cancellationToken));
    }

    public async Task<PlantWriteResult> UpdateAsync(int id, UpdatePlantRequestDto dto, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plant is null)
        {
            return new PlantWriteResult(PlantWriteStatus.NotFound);
        }

        if (dto.PlantProfileId is not null && !await ProfileExistsAsync(dto.PlantProfileId.Value, cancellationToken))
        {
            return new PlantWriteResult(PlantWriteStatus.InvalidProfile);
        }

        plant.NickName = dto.NickName.Trim();
        plant.Location = dto.Location.Trim();
        plant.PhotoUrl = dto.PhotoUrl;
        plant.AcquiredDate = dto.AcquiredDate;
        plant.CustomWateringIntervalDays = dto.CustomWateringIntervalDays;
        plant.PlantProfileId = dto.PlantProfileId;
        plant.LastWateredAt = dto.LastWateredAt;

        await db.SaveChangesAsync(cancellationToken);

        return new PlantWriteResult(PlantWriteStatus.Success, await ReloadAsync(plant.Id, cancellationToken));
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plant is null)
        {
            return false;
        }

        db.Plants.Remove(plant);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> ProfileExistsAsync(int profileId, CancellationToken cancellationToken)
        => await db.PlantProfiles.AnyAsync(p => p.Id == profileId, cancellationToken);

    private async Task<PlantResponseDto?> ReloadAsync(int id, CancellationToken cancellationToken)
    {
        var plant = await db.Plants
            .AsNoTracking()
            .Include(p => p.PlantProfile)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return plant is null ? null : ToResponse(plant);
    }

    private PlantResponseDto ToResponse(Plant plant)
    {
        var due = schedule.GetDueInfo(plant);

        return new PlantResponseDto
        {
            Id = plant.Id,
            NickName = plant.NickName,
            Location = plant.Location,
            PhotoUrl = plant.PhotoUrl,
            AcquiredDate = plant.AcquiredDate,
            PlantProfileId = plant.PlantProfileId,
            ProfileCommonName = plant.PlantProfile?.CommonName,
            CustomWateringIntervalDays = plant.CustomWateringIntervalDays,
            LastWateredAt = plant.LastWateredAt,
            DueStatus = due.Status,
            WateringIntervalDays = due.IntervalDays,
            DaysUntilDue = due.DaysUntilDue,
            NextDueDate = due.NextDueDate?.ToDateTime(TimeOnly.MinValue),
            DueMessage = due.Message,
        };
    }
}
