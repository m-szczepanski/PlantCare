using Coravel.Invocable;

namespace PlantCare.Api.Services;

/// <summary>
/// Thin Coravel adapter around <see cref="IWateringCheckService"/>; the service
/// method stays public so tests (and any future manual trigger) call it directly.
/// </summary>
public sealed class WateringCheckJob(IWateringCheckService check) : IInvocable
{
    public Task Invoke() => check.RunAsync(CancellationToken.None);
}
