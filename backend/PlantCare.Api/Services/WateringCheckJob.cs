using Coravel.Invocable;

namespace PlantCare.Api.Services;

/// <summary>Coravel entry point. Must be registered in DI: Coravel resolves scheduled invocables via GetRequiredService.</summary>
public sealed class WateringCheckJob(IWateringCheckService check) : IInvocable
{
    public Task Invoke() => check.RunAsync(CancellationToken.None);
}
