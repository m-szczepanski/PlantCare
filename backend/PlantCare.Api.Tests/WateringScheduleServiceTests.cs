using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class WateringScheduleServiceTests
{
    private readonly WateringScheduleService _service = new(new AppLocalizer("en"));

    private static readonly DateOnly Today = new(2026, 3, 15);

    private static readonly DateOnly WinterDay = new(2026, 1, 15);

    private static Plant Plant(int? intervalDays = null, DateTime? lastDoneAt = null, PlantProfile? profile = null, SoilType? soilType = null)
    {
        var plant = new Plant
        {
            NickName = "Rex",
            AcquiredDate = new DateTime(2026, 1, 1),
            PlantProfile = profile,
            SoilType = soilType,
        };
        plant.CareTasks.Add(new CareTask
        {
            Type = CareTaskType.Watering,
            IntervalDays = intervalDays,
            LastDoneAt = lastDoneAt,
        });
        return plant;
    }

    private static CareTask Watering(Plant plant) => plant.CareTasks.Single();

    [Fact]
    public void NoInterval_ReturnsNotScheduled()
    {
        var plant = Plant();

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(PlantDueStatus.NotScheduled, due.Status);
        Assert.Null(due.IntervalDays);
        Assert.Null(due.DaysUntilDue);
    }

    [Fact]
    public void DueToday_ZeroDays()
    {
        var plant = Plant(intervalDays: 7, lastDoneAt: new DateTime(2026, 3, 8));

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(PlantDueStatus.DueToday, due.Status);
        Assert.Equal(0, due.DaysUntilDue);
        Assert.Equal("Due today", due.Message);
    }

    [Fact]
    public void Overdue_NegativeDays()
    {
        var plant = Plant(intervalDays: 7, lastDoneAt: new DateTime(2026, 3, 3));

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(PlantDueStatus.Overdue, due.Status);
        Assert.Equal(-5, due.DaysUntilDue);
        Assert.Equal("5 days overdue", due.Message);
    }

    [Fact]
    public void DueTomorrow_SingularDay()
    {
        var plant = Plant(intervalDays: 7, lastDoneAt: new DateTime(2026, 3, 9));

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(PlantDueStatus.Upcoming, due.Status);
        Assert.Equal(1, due.DaysUntilDue);
        Assert.Equal("Due tomorrow", due.Message);
    }

    [Fact]
    public void NeverDone_FallsBackToAcquiredDate()
    {
        var plant = Plant(intervalDays: 7);
        plant.AcquiredDate = new DateTime(2026, 3, 8);

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(PlantDueStatus.DueToday, due.Status);
        Assert.Equal(0, due.DaysUntilDue);
    }

    [Fact]
    public void AfterMarkingDone_DueStatusResets()
    {
        var plant = Plant(intervalDays: 7, lastDoneAt: new DateTime(2026, 3, 3));

        Assert.Equal(PlantDueStatus.Overdue, _service.GetDueInfo(Watering(plant), plant, Today).Status);

        Watering(plant).LastDoneAt = Today.ToDateTime(TimeOnly.MinValue);

        var due = _service.GetDueInfo(Watering(plant), plant, Today);
        Assert.Equal(PlantDueStatus.Upcoming, due.Status);
        Assert.Equal(7, due.DaysUntilDue);
    }

    [Fact]
    public void Summer_KeepsIntervalAsIs()
    {
        var plant = Plant(intervalDays: 7, lastDoneAt: new DateTime(2026, 1, 10));
        Watering(plant).ReduceInWinter = true;

        var due = _service.GetDueInfo(Watering(plant), plant, new DateOnly(2026, 6, 15));

        Assert.Equal(7, due.IntervalDays);
        Assert.True(due.DaysUntilDue < 0);
    }

    [Fact]
    public void Winter_DoublesIntervalWhenReductionEnabled()
    {
        var plant = Plant(intervalDays: 7, lastDoneAt: new DateTime(2026, 1, 10));
        Watering(plant).ReduceInWinter = true;

        var due = _service.GetDueInfo(Watering(plant), plant, WinterDay);

        Assert.Equal(14, due.IntervalDays);
        Assert.Equal(9, due.DaysUntilDue);
    }

    [Fact]
    public void Winter_UsesProfileDefaultReductionWhenTaskDoesNotSetIt()
    {
        var profile = new PlantProfile
        {
            CommonName = "Snake Plant",
            DefaultWateringIntervalDays = 10,
            DefaultReduceInWinter = true,
            HumidityNotes = "",
            CareTips = "",
        };
        var plant = Plant(lastDoneAt: new DateTime(2026, 1, 10), profile: profile);

        var due = _service.GetDueInfo(Watering(plant), plant, WinterDay);

        Assert.Equal(20, due.IntervalDays);
    }

    [Fact]
    public void Winter_NoReductionFlag_KeepsInterval()
    {
        var plant = Plant(intervalDays: 7, lastDoneAt: new DateTime(2026, 1, 10));

        var due = _service.GetDueInfo(Watering(plant), plant, WinterDay);

        Assert.Equal(7, due.IntervalDays);
        Assert.Equal(2, due.DaysUntilDue);
    }

    [Fact]
    public void TaskIntervalOverridesProfileDefault()
    {
        var profile = new PlantProfile { CommonName = "Monstera", DefaultWateringIntervalDays = 7, HumidityNotes = "", CareTips = "" };
        var plant = Plant(intervalDays: 14, lastDoneAt: new DateTime(2026, 3, 5), profile: profile);

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(14, due.IntervalDays);
        Assert.Equal(4, due.DaysUntilDue);
    }

    [Fact]
    public void FallsBackToProfileDefaultWhenTaskHasNoInterval()
    {
        var profile = new PlantProfile { CommonName = "Snake Plant", DefaultWateringIntervalDays = 14, HumidityNotes = "", CareTips = "" };
        var plant = Plant(intervalDays: null, lastDoneAt: new DateTime(2026, 3, 11), profile: profile);

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(14, due.IntervalDays);
        Assert.Equal(10, due.DaysUntilDue);
    }

    [Fact]
    public void AllPurposeSoil_KeepsBaseInterval()
    {
        var plant = Plant(intervalDays: 10, lastDoneAt: new DateTime(2026, 3, 10), soilType: SoilType.AllPurpose);

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(10, due.IntervalDays);
        Assert.Equal(5, due.DaysUntilDue);
    }

    [Fact]
    public void NoSoilType_KeepsBaseInterval()
    {
        var plant = Plant(intervalDays: 10, lastDoneAt: new DateTime(2026, 3, 5), soilType: null);

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(10, due.IntervalDays);
    }

    [Theory]
    [InlineData(SoilType.ChunkyBark, 10, 6)]
    [InlineData(SoilType.CactusMix, 10, 7)]
    [InlineData(SoilType.PeatCoco, 10, 12)]
    [InlineData(SoilType.SemiHydro, 10, 13)]
    [InlineData(SoilType.SelfWatering, 10, 15)]
    public void SoilType_ScalesWateringInterval(SoilType soil, int baseInterval, int expected)
    {
        var plant = Plant(intervalDays: baseInterval, lastDoneAt: new DateTime(2026, 3, 5), soilType: soil);

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(expected, due.IntervalDays);
    }

    [Fact]
    public void FastDrainingSoil_CanPushAPlantToOverdue()
    {
        // 10-day schedule in fast-draining soil becomes 7 days: the 3-7 watering is now 1 day overdue.
        var plant = Plant(intervalDays: 10, lastDoneAt: new DateTime(2026, 3, 7), soilType: SoilType.CactusMix);

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(PlantDueStatus.Overdue, due.Status);
        Assert.Equal(-1, due.DaysUntilDue);
    }

    [Fact]
    public void SoilType_ScalesProfileDefaultIntervalWhenNoCustom()
    {
        var profile = new PlantProfile { CommonName = "Monstera", DefaultWateringIntervalDays = 10, HumidityNotes = "", CareTips = "" };
        var plant = Plant(intervalDays: null, lastDoneAt: new DateTime(2026, 3, 5), profile: profile, soilType: SoilType.SemiHydro);

        var due = _service.GetDueInfo(Watering(plant), plant, Today);

        Assert.Equal(13, due.IntervalDays);
    }

    [Fact]
    public void SoilType_ComposesWithWinterDoubling()
    {
        // Base 10 → SemiHydro 13 → winter doubling 26.
        var plant = Plant(intervalDays: 10, lastDoneAt: new DateTime(2026, 1, 10), soilType: SoilType.SemiHydro);
        Watering(plant).ReduceInWinter = true;

        var due = _service.GetDueInfo(Watering(plant), plant, WinterDay);

        Assert.Equal(26, due.IntervalDays);
    }

    [Fact]
    public void SoilType_DoesNotAffectFertilizingInterval()
    {
        var plant = Plant(soilType: SoilType.SelfWatering);
        plant.CareTasks.Clear();
        plant.CareTasks.Add(new CareTask
        {
            Type = CareTaskType.Fertilizing,
            IntervalDays = 30,
            LastDoneAt = new DateTime(2026, 3, 5),
        });

        var due = _service.GetDueInfo(plant.CareTasks.Single(), plant, Today);

        Assert.Equal(30, due.IntervalDays);
        Assert.Equal(20, due.DaysUntilDue);
    }
}
