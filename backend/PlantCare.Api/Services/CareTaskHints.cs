using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

/// <summary>
/// Seasonal/flush reminders for care tasks (testable pure function):
/// winter rest for fertilizing, doubled watering intervals, and a flush
/// reminder roughly every four months of feeding.
/// </summary>
public static class CareTaskHints
{
    public static string? For(CareTask task, Plant plant, DateOnly today, IAppLocalizer localizer)
    {
        var winter = CareTaskService.IsWinter(today);
        var lastDone = task.LastDoneAt is { } done ? DateOnly.FromDateTime(done.Date) : (DateOnly?)null;

        return task.Type switch
        {
            CareTaskType.Fertilizing when HealthPolicy.PausesFertilizing(plant.HealthStatus) =>
                localizer.T("hint.fertilizingPaused"),
            CareTaskType.Fertilizing when winter =>
                localizer.T("hint.fertilizingWinter"),
            CareTaskType.Fertilizing when lastDone is not null && today.DayNumber - lastDone.Value.DayNumber >= 120 =>
                localizer.T("hint.flush"),
            CareTaskType.Watering when winter && EffectiveReduce(task, plant) =>
                localizer.T("hint.wateringWinter"),
            _ => null,
        };
    }

    private static bool EffectiveReduce(CareTask task, Plant plant)
        => task.ReduceInWinter ?? plant.PlantProfile?.DefaultReduceInWinter ?? false;
}
