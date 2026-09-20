using PlantCare.Api.Models;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class CareTaskHintsTests
{
    private static readonly DateOnly Summer = new(2026, 6, 15);
    private static readonly DateOnly Winter = new(2026, 1, 15);

    private static Plant PlantWith(CareTask task, PlantProfile? profile = null)
    {
        var plant = new Plant { NickName = "Rex", AcquiredDate = new DateTime(2026, 1, 1), PlantProfile = profile };
        plant.CareTasks.Add(task);
        return plant;
    }

    [Fact]
    public void Fertilizing_InWinter_SuggestsWinterRest()
    {
        var task = new CareTask { Type = CareTaskType.Fertilizing, LastDoneAt = new DateTime(2026, 1, 10) };

        Assert.Equal(
            "Winter rest: hold off feeding until spring.",
            CareTaskHints.For(task, PlantWith(task), Winter));
    }

    [Fact]
    public void Fertilizing_GrowingSeasonOldDone_SuggestsFlush()
    {
        var task = new CareTask { Type = CareTaskType.Fertilizing, LastDoneAt = new DateTime(2026, 1, 1) };

        Assert.Contains(
            "Flushing",
            CareTaskHints.For(task, PlantWith(task), new DateOnly(2026, 6, 15)));
    }

    [Fact]
    public void Fertilizing_RecentlyDone_HasNoHint()
    {
        var task = new CareTask { Type = CareTaskType.Fertilizing, LastDoneAt = new DateTime(2026, 6, 1) };

        Assert.Null(CareTaskHints.For(task, PlantWith(task), Summer));
    }

    [Fact]
    public void Watering_InWinterWithReduction_ExplainsDoubledInterval()
    {
        var task = new CareTask { Type = CareTaskType.Watering, IntervalDays = 7, ReduceInWinter = true };

        Assert.Contains("doubled", CareTaskHints.For(task, PlantWith(task), Winter));
    }

    [Fact]
    public void Watering_UsesProfileDefaultReduction()
    {
        var task = new CareTask { Type = CareTaskType.Watering, IntervalDays = 7 };
        var profile = new PlantProfile { CommonName = "X", DefaultWateringIntervalDays = 7, DefaultReduceInWinter = true, HumidityNotes = "", CareTips = "" };

        Assert.Contains("doubled", CareTaskHints.For(task, PlantWith(task, profile), Winter));
    }
}
