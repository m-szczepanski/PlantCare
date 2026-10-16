using PlantCare.Api.Dtos;
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
    /// again a full interval later). The caller passes the watering's due info
    /// from <see cref="IWateringScheduleService"/> so the rules live in one place.
    /// </summary>
    public static void AbsorbDueWatering(Plant plant, DateTime doneAt, PlantDueInfo? wateringDue)
    {
        var wateringTask = plant.CareTasks.FirstOrDefault(t => t.Type == CareTaskType.Watering);
        if (wateringTask is not null && wateringDue?.Status is PlantDueStatus.Overdue or PlantDueStatus.DueToday)
        {
            wateringTask.LastDoneAt = doneAt;
            plant.SoilWetUntil = null;
        }

        SyncTask(plant);
    }
}
