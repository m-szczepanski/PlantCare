using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public interface ICareTaskService
{
    Task<IReadOnlyList<CareTaskResponseDto>?> ListForPlantAsync(int plantId, CancellationToken cancellationToken = default);

    Task<CareTaskResponseDto?> MarkDoneAsync(int plantId, CareTaskType type, string? note, int? amountMilliliters, WateringMethod? method, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic typed care-task operations ("mark as done" per type). Watering stays
/// a task of type Watering; the /water endpoint keeps working through it.
/// </summary>
public sealed class CareTaskService(AppDbContext db, IWateringScheduleService schedule) : ICareTaskService
{
    public async Task<IReadOnlyList<CareTaskResponseDto>?> ListForPlantAsync(int plantId, CancellationToken cancellationToken = default)
    {
        var plant = await GetPlantAsync(plantId, cancellationToken);
        if (plant is null)
        {
            return null;
        }

        return await EnsureTasksAsync(plant, cancellationToken);
    }

    public async Task<CareTaskResponseDto?> MarkDoneAsync(int plantId, CareTaskType type, string? note, int? amountMilliliters, WateringMethod? method, CancellationToken cancellationToken = default)
    {
        var plant = await GetPlantAsync(plantId, cancellationToken);
        if (plant is null)
        {
            return null;
        }

        var task = plant.CareTasks.FirstOrDefault(t => t.Type == type) ?? CreateTask(plant, type);
        var doneAt = DateTime.UtcNow;
        task.LastDoneAt = doneAt;
        task.Logs.Add(new CareTaskLog
        {
            DoneAt = doneAt,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            AmountMilliliters = amountMilliliters,
            Method = method,
        });
        await db.SaveChangesAsync(cancellationToken);

        var due = schedule.GetDueInfo(task, plant);
        return ToResponse(task, due);
    }

    private async Task<Plant?> GetPlantAsync(int plantId, CancellationToken cancellationToken)
        => await db.Plants
            .Include(p => p.PlantProfile)
            .Include(p => p.CareTasks)
            .FirstOrDefaultAsync(p => p.Id == plantId, cancellationToken);

    private async Task<IReadOnlyList<CareTaskResponseDto>> EnsureTasksAsync(Plant plant, CancellationToken cancellationToken)
    {
        if (plant.CareTasks.Count == 0)
        {
            CreateTask(plant, CareTaskType.Watering);
            await db.SaveChangesAsync(cancellationToken);
            plant = (await GetPlantAsync(plant.Id, cancellationToken))!;
        }

        return plant.CareTasks
            .OrderBy(t => t.Type)
            .Select(t => ToResponse(t, schedule.GetDueInfo(t, plant)))
            .ToList();
    }

    private CareTask CreateTask(Plant plant, CareTaskType type)
    {
        var task = new CareTask { Type = type };
        plant.CareTasks.Add(task);
        return task;
    }

    private static CareTaskResponseDto ToResponse(CareTask task, PlantDueInfo due) => new()
    {
        Id = task.Id,
        Type = task.Type,
        IntervalDays = task.IntervalDays ?? due.IntervalDays,
        LastDoneAt = task.LastDoneAt,
        DueStatus = due.Status,
        DaysUntilDue = due.DaysUntilDue,
        NextDueDate = due.NextDueDate?.ToDateTime(TimeOnly.MinValue),
        DueMessage = due.Message,
    };
}
