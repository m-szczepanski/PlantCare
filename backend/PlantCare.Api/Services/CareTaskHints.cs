using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

/// <summary>
/// Seasonal/flush reminders for care tasks (testable pure function):
/// winter rest for fertilizing, doubled watering intervals, and a flush
/// reminder roughly every four months of feeding.
/// </summary>
public static class CareTaskHints
{
    public static string? For(CareTask task, Plant plant, DateOnly today)
    {
        var winter = CareTaskService.IsWinter(today);
        var lastDone = task.LastDoneAt is { } done ? DateOnly.FromDateTime(done.Date) : (DateOnly?)null;

        return task.Type switch
        {
            CareTaskType.Fertilizing when winter =>
                "Winter rest: hold off feeding until spring.",
            CareTaskType.Fertilizing when lastDone is not null && today.DayNumber - lastDone.Value.DayNumber >= 120 =>
                "Flushing the soil with plain water helps clear fertilizer salts.",
            CareTaskType.Watering when winter && EffectiveReduce(task, plant) =>
                "Winter: watering interval is doubled.",
            _ => null,
        };
    }

    private static bool EffectiveReduce(CareTask task, Plant plant)
        => task.ReduceInWinter ?? plant.PlantProfile?.DefaultReduceInWinter ?? false;
}
