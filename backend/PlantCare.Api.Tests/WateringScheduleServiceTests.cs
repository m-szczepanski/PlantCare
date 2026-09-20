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

    private static Plant Plant(int? intervalDays = null, DateTime? lastDoneAt = null, PlantProfile? profile = null)
    {
        var plant = new Plant
        {
            NickName = "Rex",
            AcquiredDate = new DateTime(2026, 1, 1),
            PlantProfile = profile,
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
}
