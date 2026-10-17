using Microsoft.Extensions.Configuration;
using PlantCare.Api.Configuration;
using Xunit;

namespace PlantCare.Api.Tests;

public class PlantCareConfigurationTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("70000")]
    [InlineData("not-a-port")]
    public void Port_fails_fast_on_a_malformed_value(string raw)
    {
        var configuration = Build(("MQTT_PORT", raw));

        Assert.Throws<InvalidOperationException>(() => PlantCareConfiguration.Port(configuration, "MQTT_PORT", 1883));
    }

    [Fact]
    public void Port_falls_back_when_unset()
    {
        var configuration = Build();

        Assert.Equal(1883, PlantCareConfiguration.Port(configuration, "MQTT_PORT", 1883));
    }

    [Fact]
    public void OptionalPort_returns_null_when_unset()
    {
        var configuration = Build();

        Assert.Null(PlantCareConfiguration.OptionalPort(configuration, "NTFY_PUBLIC_PORT"));
    }

    private static IConfiguration Build(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();
}
