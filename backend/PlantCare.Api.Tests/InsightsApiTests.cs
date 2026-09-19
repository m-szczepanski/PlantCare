using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;
using Xunit;

namespace PlantCare.Api.Tests;

public class InsightsApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TempDatabase _database = new();
    private readonly HttpClient _client;

    public InsightsApiTests()
    {
        var factory = _database.CreateFactory();
        _client = factory.CreateClient();
    }

    private async Task<InsightsResponseDto> GetInsights()
    {
        var response = await _client.GetFromJsonAsync<InsightsResponseDto>("/api/insights", Options);
        Assert.NotNull(response);
        return response;
    }

    private async Task<int> CreatePlant(string nickName, int? interval, DateTime? lastWateredAt = null, int? profileId = null)
    {
        var created = await _client.PostAsJsonAsync("/api/plants", new
        {
            nickName,
            location = "Kitchen",
            customWateringIntervalDays = interval,
            lastWateredAt,
            plantProfileId = profileId,
        }, Options);
        var plant = await created.Content.ReadFromJsonAsync<PlantResponseDto>(Options);
        return plant!.Id;
    }

    private async Task<int> CreateProfile(string commonName)
    {
        var response = await _client.PostAsJsonAsync("/api/plant-profiles", new PlantProfileRequestDto
        {
            CommonName = commonName,
            ScientificName = null,
            DefaultWateringIntervalDays = 10,
            LightRequirement = LightRequirement.Medium,
            HumidityNotes = "Average.",
            CareTips = "Keep it simple.",
        }, Options);
        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<PlantProfileResponseDto>(Options);
        return profile!.Id;
    }

    [Fact]
    public async Task Insights_EmptyDatabase_ReturnsZeroedSummary()
    {
        var insights = await GetInsights();

        Assert.Equal(0, insights.TotalPlants);
        Assert.Equal(0, insights.ScheduledPlants);
        Assert.Equal(0, insights.SpeciesCount);
        Assert.Equal(0, insights.AdherencePercent);
        Assert.Equal(0, insights.ActualWateringsInWindow);
        Assert.Equal(12, insights.MonthlyWaterings.Count);
    }

    [Fact]
    public async Task Insights_CountsTotalsSpeciesAndNeglect()
    {
        var profileId = await CreateProfile("Insights Pothos");
        await CreatePlant("Fresh Freddie", 7, DateTime.UtcNow.Date, profileId);
        await CreatePlant("Ignored Ivy", 7, DateTime.UtcNow.Date.AddDays(-40), profileId);
        await CreatePlant("Free Spirit", null);

        var insights = await GetInsights();

        Assert.Equal(3, insights.TotalPlants);
        Assert.Equal(2, insights.ScheduledPlants);
        Assert.Equal(1, insights.UnscheduledPlants);
        Assert.Equal(1, insights.SpeciesCount);
        Assert.Equal(new[] { profileId }, insights.Species.Select(s => s.PlantProfileId));
        Assert.Equal(2, insights.Species[0].PlantCount);
        Assert.Equal("Ignored Ivy", insights.MostNeglected[0].NickName);
        Assert.InRange(insights.MostNeglected[0].DaysSinceLastWatering, 39, 41);
    }

    [Fact]
    public async Task Insights_AdherenceReflectsRecentWaterings()
    {
        var id = await CreatePlant("Diligent Dan", 10, DateTime.UtcNow.Date.AddDays(-10));

        await _client.PostAsJsonAsync($"/api/plants/{id}/water", new { }, Options);
        await _client.PostAsJsonAsync($"/api/plants/{id}/water", new { }, Options);

        var insights = await GetInsights();

        Assert.Equal(30, insights.AdherenceWindowDays);
        Assert.Equal(3, insights.ExpectedWateringsInWindow);
        Assert.Equal(2, insights.ActualWateringsInWindow);
        Assert.InRange(insights.AdherencePercent, 65, 68);
        Assert.Equal(new[] { 2 }, insights.Streaks.Select(s => s.ConsecutiveOnTimeWaterings));
    }

    [Fact]
    public async Task Insights_StreakBreaksWhenGapExceedsIntervalTolerance()
    {
        var id = await CreatePlant("Sporadic Sam", 5, DateTime.UtcNow.Date.AddDays(-20));
        await _client.PostAsJsonAsync($"/api/plants/{id}/water", new { }, Options);

        var insights = await GetInsights();

        Assert.Equal(new[] { 1 }, insights.Streaks.Select(s => s.ConsecutiveOnTimeWaterings));
    }

    [Fact]
    public async Task Insights_MonthlyWateringsIncludeCurrentMonth()
    {
        var id = await CreatePlant("Watery Wendy", 7, DateTime.UtcNow.Date.AddDays(-1));
        await _client.PostAsJsonAsync($"/api/plants/{id}/water", new { }, Options);

        var insights = await GetInsights();

        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        Assert.Equal(currentMonth, insights.MonthlyWaterings[^1].Month);
        Assert.True(insights.MonthlyWaterings[^1].Count >= 1);
    }

    public void Dispose()
    {
        _client.Dispose();
        _database.Dispose();
    }
}
