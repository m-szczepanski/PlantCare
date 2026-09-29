using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

/// <summary>
/// Keeps the derived "top up water" care task in sync with the plant's soil:
/// highly permeable substrates (see <see cref="SoilTypes.IsHighlyPermeable"/>)
/// get a <see cref="CareTaskType.TopUpWatering"/> task row; switching to a
/// slower mix removes it. A task the user removed deliberately is not
/// resurrected until the soil changes again.
/// </summary>
public static class TopUpWatering
{
    public static void SyncTask(Plant plant)
    {
        var existing = plant.CareTasks.FirstOrDefault(t => t.Type == CareTaskType.TopUpWatering);

        if (SoilTypes.IsHighlyPermeable(plant.SoilType))
        {
            if (existing is null)
            {
                plant.CareTasks.Add(new CareTask { Type = CareTaskType.TopUpWatering });
            }
        }
        else if (existing is not null)
        {
            plant.CareTasks.Remove(existing);
        }
    }

    /// <summary>
    /// Marking a top-up done keeps the two actions from competing: when the
    /// full watering is already due/overdue, "topping up" is the same drink —
    /// the watering anchor moves to now (no log row; the watering stays due
    /// again a full interval later).
    /// </summary>
    public static void AbsorbDueWatering(Plant plant, CareTask topUpTask, DateTime doneAt)
    {
        var wateringTask = plant.CareTasks.FirstOrDefault(t => t.Type == CareTaskType.Watering);
        if (wateringTask is null)
        {
            return;
        }

        var today = DateOnly.FromDateTime(doneAt.Date);
        var due = WateringDueDate(wateringTask, plant, today);
        if (due is not null && due.Value.DayNumber - today.DayNumber <= 0)
        {
            wateringTask.LastDoneAt = doneAt;
            plant.SoilWetUntil = null;
        }

        SyncTask(plant);
    }

    private static DateOnly? WateringDueDate(CareTask wateringTask, Plant plant, DateOnly today)
    {
        var intervalDays = wateringTask.IntervalDays ?? plant.PlantProfile?.DefaultWateringIntervalDays;
        if (intervalDays is null or <= 0)
        {
            return null;
        }

        var effective = HealthPolicy.AdjustWateringInterval(plant.HealthStatus, SoilTypes.AdjustInterval(plant.SoilType, intervalDays.Value));
        var reduceInWinter = wateringTask.ReduceInWinter ?? plant.PlantProfile?.DefaultReduceInWinter ?? false;
        if (reduceInWinter && CareTaskService.IsWinter(today))
        {
            effective *= 2;
        }

        var anchor = DateOnly.FromDateTime((wateringTask.LastDoneAt ?? plant.AcquiredDate).Date);
        return anchor.AddDays(effective);
    }
}
