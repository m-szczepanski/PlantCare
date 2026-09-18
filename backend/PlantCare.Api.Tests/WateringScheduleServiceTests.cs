using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class WateringScheduleServiceTests
{
    private readonly WateringScheduleService _service = new();

    private static readonly DateOnly Today = new(2026, 3, 15);

    [Fact]
    public void NoInterval_ReturnsNotScheduled()
    {
        var plant = new Plant { NickName = "Rex", Location = "Desk", AcquiredDate = Today.ToDateTime(TimeOnly.MinValue) };

        var due = _service.GetDueInfo(plant, Today);

        Assert.Equal(PlantDueStatus.NotScheduled, due.Status);
        Assert.Null(due.IntervalDays);
        Assert.Null(due.DaysUntilDue);
    }

    [Fact]
    public void DueToday_ZeroDays()
    {
        var plant = new Plant
        {
            NickName = "Rex",
            Location = "Desk",
            AcquiredDate = new DateTime(2026, 1, 1),
            CustomWateringIntervalDays = 7,
            LastWateredAt = new DateTime(2026, 3, 8),
        };

        var due = _service.GetDueInfo(plant, Today);

        Assert.Equal(PlantDueStatus.DueToday, due.Status);
        Assert.Equal(0, due.DaysUntilDue);
        Assert.Equal("Due today", due.Message);
    }

    [Fact]
    public void Overdue_NegativeDays()
    {
        var plant = new Plant
        {
            NickName = "Rex",
            Location = "Desk",
            CustomWateringIntervalDays = 7,
            LastWateredAt = new DateTime(2026, 3, 3),
        };

        var due = _service.GetDueInfo(plant, Today);

        Assert.Equal(PlantDueStatus.Overdue, due.Status);
        Assert.Equal(-5, due.DaysUntilDue);
        Assert.Equal("5 days overdue", due.Message);
    }

    [Fact]
    public void DueTomorrow_SingularDay()
    {
        var plant = new Plant
        {
            NickName = "Rex",
            Location = "Desk",
            CustomWateringIntervalDays = 7,
            LastWateredAt = new DateTime(2026, 3, 9),
        };

        var due = _service.GetDueInfo(plant, Today);

        Assert.Equal(PlantDueStatus.Upcoming, due.Status);
        Assert.Equal(1, due.DaysUntilDue);
        Assert.Equal("Due tomorrow", due.Message);
    }

    [Fact]
    public void NeverWatered_FallsBackToAcquiredDate()
    {
        var plant = new Plant
        {
            NickName = "Rex",
            Location = "Desk",
            AcquiredDate = new DateTime(2026, 3, 8),
            CustomWateringIntervalDays = 7,
            LastWateredAt = null,
        };

        var due = _service.GetDueInfo(plant, Today);

        Assert.Equal(PlantDueStatus.DueToday, due.Status);
        Assert.Equal(0, due.DaysUntilDue);
    }

    [Fact]
    public void CustomIntervalOverridesProfileDefault()
    {
        var plant = new Plant
        {
            NickName = "Rex",
            Location = "Desk",
            PlantProfile = new PlantProfile { CommonName = "Monstera", DefaultWateringIntervalDays = 7, HumidityNotes = "", CareTips = "" },
            CustomWateringIntervalDays = 14,
            LastWateredAt = new DateTime(2026, 3, 5),
        };

        var due = _service.GetDueInfo(plant, Today);

        Assert.Equal(14, due.IntervalDays);
        Assert.Equal(4, due.DaysUntilDue);
    }

    [Fact]
    public void FallsBackToProfileDefaultWhenNoCustom()
    {
        var plant = new Plant
        {
            NickName = "Rex",
            Location = "Desk",
            PlantProfile = new PlantProfile { CommonName = "Snake Plant", DefaultWateringIntervalDays = 14, HumidityNotes = "", CareTips = "" },
            CustomWateringIntervalDays = null,
            LastWateredAt = new DateTime(2026, 3, 11),
        };

        var due = _service.GetDueInfo(plant, Today);

        Assert.Equal(14, due.IntervalDays);
        Assert.Equal(10, due.DaysUntilDue);
    }
}
