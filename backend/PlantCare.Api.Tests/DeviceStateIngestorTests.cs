using PlantCare.Api.Models;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class DeviceStateIngestorTests
{
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

    [Fact]
    public void DeviceStateMessage_TryParse_Watered()
    {
        Assert.True(DeviceStateMessage.TryParse("{\"watered\":true,\"moisturePct\":70}")!.Watered);
        Assert.False(DeviceStateMessage.TryParse("{\"moisturePct\":70}")!.Watered);
        Assert.False(DeviceStateMessage.TryParse("{\"watered\":false}")!.Watered);
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
}
