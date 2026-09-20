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
    InvalidRoom,
}

public sealed record PlantWriteResult(PlantWriteStatus Status, PlantResponseDto? Plant = null);

public enum PlantPhotoStatus
{
    Success,
    NotFound,
    InvalidFile,
}

public sealed record PlantPhotoResult(PlantPhotoStatus Status, PlantResponseDto? Plant = null, string? Error = null);

public interface IPlantService
{
    Task<IReadOnlyList<PlantResponseDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<PlantResponseDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<PlantWriteResult> CreateAsync(CreatePlantRequestDto dto, CancellationToken cancellationToken = default);

    Task<PlantWriteResult> UpdateAsync(int id, UpdatePlantRequestDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<PlantResponseDto?> WaterAsync(int id, string? note, int? amountMilliliters = null, WateringMethod? method = null, CancellationToken cancellationToken = default);

    Task<PlantResponseDto?> UndoWaterAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WateringLogResponseDto>?> GetWateringHistoryAsync(int id, CancellationToken cancellationToken = default);

    Task<PlantPhotoResult> UploadPhotoAsync(int id, Stream content, string? contentType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlantNoteResponseDto>?> GetNotesAsync(int id, CancellationToken cancellationToken = default);

    Task<PlantNoteResponseDto?> AddNoteAsync(int id, string text, CancellationToken cancellationToken = default);

    Task<PlantResponseDto?> SetSnoozeAsync(int id, int days, CancellationToken cancellationToken = default);

    Task<PlantResponseDto?> ClearSnoozeAsync(int id, CancellationToken cancellationToken = default);

    Task<int> SnoozeAllAsync(int days, CancellationToken cancellationToken = default);
}

public sealed class PlantService(AppDbContext db, IWateringScheduleService schedule, FeatureFlags features, IPlantPhotoStorage photos) : IPlantService
{
    public async Task<IReadOnlyList<PlantResponseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var plants = await db.Plants
            .Include(p => p.PlantProfile)
            .Include(p => p.Room)
            .Include(p => p.CareTasks)
            .OrderBy(p => p.NickName)
            .ToListAsync(cancellationToken);

        return plants.Select(ToResponse).ToList();
    }

    public async Task<PlantResponseDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants
            .Include(p => p.PlantProfile)
            .Include(p => p.Room)
            .Include(p => p.CareTasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return plant is null ? null : ToResponse(plant);
    }

    public async Task<PlantWriteResult> CreateAsync(CreatePlantRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.PlantProfileId is not null && !await ProfileExistsAsync(dto.PlantProfileId.Value, cancellationToken))
        {
            return new PlantWriteResult(PlantWriteStatus.InvalidProfile);
        }

        if (dto.RoomId is not null && !await RoomExistsAsync(dto.RoomId.Value, cancellationToken))
        {
            return new PlantWriteResult(PlantWriteStatus.InvalidRoom);
        }

        var plant = new Plant
        {
            NickName = dto.NickName.Trim(),
            RoomId = dto.RoomId,
            PhotoUrl = dto.PhotoUrl,
            PotSizeCm = dto.PotSizeCm,
            SoilMix = string.IsNullOrWhiteSpace(dto.SoilMix) ? null : dto.SoilMix.Trim(),
            PropagatedFrom = string.IsNullOrWhiteSpace(dto.PropagatedFrom) ? null : dto.PropagatedFrom.Trim(),
            AcquiredDate = dto.AcquiredDate,
            PlantProfileId = dto.PlantProfileId,
            NotifyEnabled = dto.NotifyEnabled,
        };
        plant.CareTasks.Add(new CareTask
        {
            Type = CareTaskType.Watering,
            IntervalDays = dto.CustomWateringIntervalDays,
            LastDoneAt = dto.LastWateredAt,
            ReduceInWinter = dto.ReduceInWinter ?? false,
        });

        db.Plants.Add(plant);
        await db.SaveChangesAsync(cancellationToken);

        return new PlantWriteResult(PlantWriteStatus.Success, await ReloadAsync(plant.Id, cancellationToken));
    }

    public async Task<PlantWriteResult> UpdateAsync(int id, UpdatePlantRequestDto dto, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants
            .Include(p => p.CareTasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plant is null)
        {
            return new PlantWriteResult(PlantWriteStatus.NotFound);
        }

        if (dto.PlantProfileId is not null && !await ProfileExistsAsync(dto.PlantProfileId.Value, cancellationToken))
        {
            return new PlantWriteResult(PlantWriteStatus.InvalidProfile);
        }

        if (dto.RoomId is not null && !await RoomExistsAsync(dto.RoomId.Value, cancellationToken))
        {
            return new PlantWriteResult(PlantWriteStatus.InvalidRoom);
        }

        plant.NickName = dto.NickName.Trim();
        plant.RoomId = dto.RoomId;
        plant.PhotoUrl = dto.PhotoUrl;
        plant.PotSizeCm = dto.PotSizeCm;
        plant.SoilMix = string.IsNullOrWhiteSpace(dto.SoilMix) ? null : dto.SoilMix.Trim();
        plant.PropagatedFrom = string.IsNullOrWhiteSpace(dto.PropagatedFrom) ? null : dto.PropagatedFrom.Trim();
        plant.NotifyEnabled = dto.NotifyEnabled;
        plant.AcquiredDate = dto.AcquiredDate;
        plant.PlantProfileId = dto.PlantProfileId;

        var watering = WateringTask(plant) ?? NewWateringTask(plant);
        watering.IntervalDays = dto.CustomWateringIntervalDays;
        watering.LastDoneAt = dto.LastWateredAt;
        watering.ReduceInWinter = dto.ReduceInWinter ?? watering.ReduceInWinter;

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
        photos.DeletePlantDirectory(plant.Id);
        return true;
    }

    /// <summary>
    /// Logs a watering event and updates <see cref="Plant.LastWateredAt"/> atomically:
    /// both changes go out in the single implicit transaction of one SaveChanges call.
    /// </summary>
    public async Task<PlantResponseDto?> WaterAsync(int id, string? note, int? amountMilliliters = null, WateringMethod? method = null, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants
            .Include(p => p.PlantProfile)
            .Include(p => p.Room)
            .Include(p => p.CareTasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (plant is null)
        {
            return null;
        }

        var wateredAt = DateTime.UtcNow;
        var watering = WateringTask(plant) ?? NewWateringTask(plant);
        watering.LastDoneAt = wateredAt;
        watering.Logs.Add(new CareTaskLog
        {
            DoneAt = wateredAt,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            AmountMilliliters = amountMilliliters,
            Method = method,
        });

        await db.SaveChangesAsync(cancellationToken);

        return await ReloadAsync(plant.Id, cancellationToken);
    }

    /// <summary>
    /// Removes the most recent watering log and rewinds <see cref="Plant.LastWateredAt"/>
    /// to the next newest entry (or null). A no-op when the plant has no logs.
    /// </summary>
    public async Task<PlantResponseDto?> UndoWaterAsync(int id, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants
            .Include(p => p.PlantProfile)
            .Include(p => p.Room)
            .Include(p => p.CareTasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (plant is null)
        {
            return null;
        }

        var watering = WateringTask(plant);
        if (watering is not null)
        {
            var latest = await db.CareTaskLogs
                .Where(w => w.CareTaskId == watering.Id)
                .OrderByDescending(w => w.DoneAt)
                .ThenByDescending(w => w.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest is not null)
            {
                db.CareTaskLogs.Remove(latest);

                var previous = await db.CareTaskLogs
                    .Where(w => w.CareTaskId == watering.Id && w.Id != latest.Id)
                    .OrderByDescending(w => w.DoneAt)
                    .ThenByDescending(w => w.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                watering.LastDoneAt = previous?.DoneAt;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return ToResponse(plant);
    }

    public async Task<IReadOnlyList<WateringLogResponseDto>?> GetWateringHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await db.Plants.AnyAsync(p => p.Id == id, cancellationToken))
        {
            return null;
        }

        var watering = await db.CareTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.PlantId == id && t.Type == CareTaskType.Watering, cancellationToken);

        if (watering is null)
        {
            return [];
        }

        return await db.CareTaskLogs
            .AsNoTracking()
            .Where(w => w.CareTaskId == watering.Id)
            .OrderByDescending(w => w.DoneAt)
            .ThenByDescending(w => w.Id)
            .Select(w => new WateringLogResponseDto
            {
                Id = w.Id,
                WateredAt = w.DoneAt,
                Note = w.Note,
                AmountMilliliters = w.AmountMilliliters,
                Method = w.Method,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PlantPhotoResult> UploadPhotoAsync(int id, Stream content, string? contentType, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plant is null)
        {
            return new PlantPhotoResult(PlantPhotoStatus.NotFound);
        }

        string photoUrl;
        try
        {
            photoUrl = await photos.SaveAsync(id, content, contentType ?? string.Empty, cancellationToken);
        }
        catch (InvalidDataException ex)
        {
            return new PlantPhotoResult(PlantPhotoStatus.InvalidFile, Error: ex.Message);
        }

        var previousUrl = plant.PhotoUrl;
        plant.PhotoUrl = photoUrl;
        await db.SaveChangesAsync(cancellationToken);

        photos.DeleteIfManaged(previousUrl);

        return new PlantPhotoResult(PlantPhotoStatus.Success, await ReloadAsync(plant.Id, cancellationToken));
    }

    public async Task<PlantResponseDto?> SetSnoozeAsync(int id, int days, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants
            .Include(p => p.PlantProfile)
            .Include(p => p.Room)
            .Include(p => p.CareTasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plant is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var from = plant.SnoozedUntil is { } existing && existing > now ? existing : now;
        plant.SnoozedUntil = from.AddDays(days);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(plant);
    }

    public async Task<PlantResponseDto?> ClearSnoozeAsync(int id, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants
            .Include(p => p.PlantProfile)
            .Include(p => p.Room)
            .Include(p => p.CareTasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plant is null)
        {
            return null;
        }

        plant.SnoozedUntil = null;
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(plant);
    }

    public async Task<int> SnoozeAllAsync(int days, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var until = now.AddDays(days);
        var plants = await db.Plants.ToListAsync(cancellationToken);

        foreach (var plant in plants)
        {
            plant.SnoozedUntil = plant.SnoozedUntil is { } existing && existing > until ? existing : until;
        }

        await db.SaveChangesAsync(cancellationToken);
        return plants.Count;
    }

    public async Task<IReadOnlyList<PlantNoteResponseDto>?> GetNotesAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await db.Plants.AnyAsync(p => p.Id == id, cancellationToken))
        {
            return null;
        }

        return await db.PlantNotes
            .AsNoTracking()
            .Where(n => n.PlantId == id)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Select(n => new PlantNoteResponseDto
            {
                Id = n.Id,
                CreatedAt = n.CreatedAt,
                Text = n.Text,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PlantNoteResponseDto?> AddNoteAsync(int id, string text, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plant is null)
        {
            return null;
        }

        var note = new PlantNote
        {
            PlantId = plant.Id,
            CreatedAt = DateTime.UtcNow,
            Text = text.Trim(),
        };
        db.PlantNotes.Add(note);
        await db.SaveChangesAsync(cancellationToken);

        return new PlantNoteResponseDto
        {
            Id = note.Id,
            CreatedAt = note.CreatedAt,
            Text = note.Text,
        };
    }

    private async Task<bool> ProfileExistsAsync(int profileId, CancellationToken cancellationToken)
        => await db.PlantProfiles.AnyAsync(p => p.Id == profileId, cancellationToken);

    private async Task<bool> RoomExistsAsync(int roomId, CancellationToken cancellationToken)
        => await db.Rooms.AnyAsync(r => r.Id == roomId, cancellationToken);

    private static CareTask? WateringTask(Plant plant)
        => plant.CareTasks.FirstOrDefault(t => t.Type == CareTaskType.Watering);

    private static CareTask NewWateringTask(Plant plant)
    {
        var task = new CareTask { Type = CareTaskType.Watering };
        plant.CareTasks.Add(task);
        return task;
    }

    private async Task<PlantResponseDto?> ReloadAsync(int id, CancellationToken cancellationToken)
    {
        var plant = await db.Plants
            .AsNoTracking()
            .Include(p => p.PlantProfile)
            .Include(p => p.Room)
            .Include(p => p.CareTasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return plant is null ? null : ToResponse(plant);
    }

    private PlantResponseDto ToResponse(Plant plant)
    {
        var watering = WateringTask(plant);
        var due = schedule.GetDueInfo(watering, plant);

        return new PlantResponseDto
        {
            Id = plant.Id,
            NickName = plant.NickName,
            RoomId = plant.RoomId,
            RoomName = plant.Room?.Name,
            PhotoUrl = plant.PhotoUrl,
            PotSizeCm = plant.PotSizeCm,
            SoilMix = plant.SoilMix,
            PropagatedFrom = plant.PropagatedFrom,
            NotifyEnabled = plant.NotifyEnabled,
            SnoozedUntil = plant.SnoozedUntil,
            AcquiredDate = plant.AcquiredDate,
            PlantProfileId = plant.PlantProfileId,
            ProfileCommonName = plant.PlantProfile?.CommonName,
            ProfileToxicToPets = plant.PlantProfile?.ToxicToPets ?? false,
            ProfileToxicToChildren = plant.PlantProfile?.ToxicToChildren ?? false,
            CareTips = features.CareTipsEnabled && plant.PlantProfile is { } profile
                ? new PlantCareTipsDto
                {
                    CommonName = profile.CommonName,
                    LightRequirement = profile.LightRequirement.ToString(),
                    HumidityNotes = profile.HumidityNotes,
                    CareTips = profile.CareTips,
                }
                : null,
            CustomWateringIntervalDays = watering?.IntervalDays,
            ReduceInWinter = watering?.ReduceInWinter,
            LastWateredAt = watering?.LastDoneAt,
            DueStatus = due.Status,
            WateringIntervalDays = due.IntervalDays,
            DaysUntilDue = due.DaysUntilDue,
            NextDueDate = due.NextDueDate?.ToDateTime(TimeOnly.MinValue),
            DueMessage = due.Message,
            RoomLightMatch = ComputeRoomLightMatch(plant),
        };
    }

    private static RoomLightMatch? ComputeRoomLightMatch(Plant plant)
    {
        if (plant.PlantProfile is null || plant.Room?.LightExposure is not { } exposure)
        {
            return null;
        }

        var diff = (int)exposure - (int)plant.PlantProfile.LightRequirement;
        return diff switch
        {
            0 => RoomLightMatch.Good,
            1 => RoomLightMatch.SlightlyTooBright,
            -1 => RoomLightMatch.SlightlyTooDark,
            >= 2 => RoomLightMatch.MuchTooBright,
            <= -2 => RoomLightMatch.MuchTooDark,
        };
    }
}
