using Microsoft.Extensions.Logging.Abstractions;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class DeviceBusTests
{
    [Theory]
    [InlineData("plantcare", 3, "plantcare/3/command")]
    [InlineData("zigbee2mqtt", 42, "zigbee2mqtt/42/command")]
    public void DeviceTopics_Command(string prefix, int id, string expected)
        => Assert.Equal(expected, DeviceTopics.Command(prefix, id));

    [Fact]
    public void DeviceTopics_StateAndPattern()
    {
        Assert.Equal("plantcare/7/state", DeviceTopics.State("plantcare", 7));
        Assert.Equal("plantcare/+/state", DeviceTopics.StatePattern("plantcare"));
    }

    [Fact]
    public async Task PublishAsync_Throws_WhenNotConnected()
    {
        using var bus = new MqttDeviceBus(
            new MqttOptions("127.0.0.1", 1, null, null, "plantcare"),
            NullLogger<MqttDeviceBus>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync("plantcare/1/command", "{}"));
    }

    [Fact]
    public async Task Bus_DoesNotCrash_WhenBrokerUnreachable()
    {
        using var bus = new MqttDeviceBus(
            new MqttOptions("127.0.0.1", 1, null, null, "plantcare"),
            NullLogger<MqttDeviceBus>.Instance);
        using var cts = new CancellationTokenSource();

        await bus.StartAsync(cts.Token);
        await Task.Delay(1500);

        Assert.False(bus.IsConnected);

        cts.Cancel();
        await bus.StopAsync(CancellationToken.None);
    }
}
