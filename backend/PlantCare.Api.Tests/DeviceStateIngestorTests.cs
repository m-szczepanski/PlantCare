using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PlantCare.Api.Data;
using PlantCare.Api.Models;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class DeviceStateIngestorTests
{
    private static DeviceStateOptions Options => new(WetThresholdPct: 60, DeferDays: 1);

    [Theory]
    [InlineData("{\"moisturePct\":42,\"waterLevel\":\"ok\",\"ts\":\"2026-10-15T08:00:00Z\"}", 42, DeviceWaterLevel.Ok)]
    [InlineData("{\"moisturePct\":10,\"waterLevel\":\"LOW\"}", 10, DeviceWaterLevel.Low)]
    [InlineData("{\"waterLevel\":\"empty\"}", null, DeviceWaterLevel.Empty)]
    [InlineData("{\"moisturePct\":70}", 70, null)]
    public void DeviceStateMessage_TryParse(string payload, int? moisture, DeviceWaterLevel? waterLevel)
    {
        var state = DeviceStateMessage.TryParse(payload);

        Assert.NotNull(state);
        Assert.Equal(moisture, state.MoisturePct);
        Assert.Equal(waterLevel, state.WaterLevel);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("\"just a string\"")]
    public void DeviceStateMessage_TryParse_Invalid_ReturnsNull(string payload)
        => Assert.Null(DeviceStateMessage.TryParse(payload));

    [Theory]
    [InlineData("plantcare/7/state", 7)]
    [InlineData("zigbee2mqtt/42/state", 42)]
    public void DeviceTopics_TryParseStateTopic_Valid(string topic, int id)
    {
        Assert.True(DeviceTopics.TryParseStateTopic(topic, out var parsed));
        Assert.Equal(id, parsed);
    }

    [Theory]
    [InlineData("plantcare/7/command")]
    [InlineData("plantcare/state")]
    [InlineData("plantcare/abc/state")]
    [InlineData("plantcare/7")]
    public void DeviceTopics_TryParseStateTopic_Invalid(string topic)
        => Assert.False(DeviceTopics.TryParseStateTopic(topic, out _));

    [Fact]
    public async Task Ingest_WetThenDry_DefersThenClears()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"plantcare-ingest-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={dbPath}").Options;
        try
        {
            using var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var device = new Device { Name = "Sensor 1", Type = DeviceType.Sensor };
            db.Devices.Add(device);
            await db.SaveChangesAsync();

            var plant = new Plant { NickName = "Fern", DeviceId = device.Id, AcquiredDate = DateTime.UtcNow };
            db.Plants.Add(plant);
            await db.SaveChangesAsync();

            var ingestor = new DeviceStateIngestor(db, Options, NullLogger<DeviceStateIngestor>.Instance);

            // Wet reading → device reading mirrored + plant deferred.
            await ingestor.IngestAsync($"plantcare/{device.Id}/state", "{\"moisturePct\":70,\"waterLevel\":\"ok\"}");
            db.ChangeTracker.Clear();

            Assert.Equal(70, (await db.Devices.FirstAsync(d => d.Id == device.Id)).LastMoisturePct);
            Assert.Equal(DeviceWaterLevel.Ok, (await db.Devices.FirstAsync(d => d.Id == device.Id)).WaterLevel);
            Assert.NotNull((await db.Plants.FirstAsync(p => p.Id == plant.Id)).SoilWetUntil);

            // Dry reading → deferral cleared.
            await ingestor.IngestAsync($"plantcare/{device.Id}/state", "{\"moisturePct\":30}");
            db.ChangeTracker.Clear();

            Assert.Null((await db.Plants.FirstAsync(p => p.Id == plant.Id)).SoilWetUntil);
        }
        finally
        {
            foreach (var suffix in new[] { "", "-shm", "-wal" })
            {
                try
                {
                    File.Delete(dbPath + suffix);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
