using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public interface IHealthCheckService
{
    Task<IReadOnlyList<HealthCheckResponseDto>?> ListAsync(int plantId, CancellationToken cancellationToken = default);

    Task<PlantResponseDto?> AddAsync(int plantId, HealthStatus status, string? note, CancellationToken cancellationToken = default);
}

/// <summary>
/// Monthly health checkups: an immutable answer per call; the newest one becomes
/// the plant's current status (feeding back into the schedule via
/// <see cref="HealthPolicy"/> and <see cref="IWateringScheduleService"/>).
/// </summary>
public sealed class HealthCheckService(AppDbContext db, IPlantService plants) : IHealthCheckService
{
    public async Task<IReadOnlyList<HealthCheckResponseDto>?> ListAsync(int plantId, CancellationToken cancellationToken = default)
    {
        if (!await db.Plants.AnyAsync(p => p.Id == plantId, cancellationToken))
        {
            return null;
        }

        return await db.PlantHealthChecks
            .AsNoTracking()
            .Where(h => h.PlantId == plantId)
            .OrderByDescending(h => h.CheckedAt)
            .ThenByDescending(h => h.Id)
            .Select(h => new HealthCheckResponseDto
            {
                Id = h.Id,
                Status = h.Status,
                CheckedAt = h.CheckedAt,
                Note = h.Note,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PlantResponseDto?> AddAsync(int plantId, HealthStatus status, string? note, CancellationToken cancellationToken = default)
    {
        var plant = await db.Plants.FirstOrDefaultAsync(p => p.Id == plantId, cancellationToken);
        if (plant is null)
        {
            return null;
        }

        var checkedAt = DateTime.UtcNow;
        db.PlantHealthChecks.Add(new PlantHealthCheck
        {
            PlantId = plant.Id,
            CheckedAt = checkedAt,
            Status = status,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        });

        plant.HealthStatus = status;
        plant.LastCheckupAt = checkedAt;

        await db.SaveChangesAsync(cancellationToken);

        return await plants.GetAsync(plantId, cancellationToken);
    }
}
