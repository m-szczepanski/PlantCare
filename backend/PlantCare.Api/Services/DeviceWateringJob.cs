using Coravel.Invocable;

namespace PlantCare.Api.Services;

/// <summary>Coravel entry point. Must be registered in DI: Coravel resolves scheduled invocables via
/// GetRequiredService.</summary>
public sealed class DeviceWateringJob(IDeviceWateringService watering) : IInvocable
{
    public Task Invoke() => watering.RunAsync(CancellationToken.None);
}
