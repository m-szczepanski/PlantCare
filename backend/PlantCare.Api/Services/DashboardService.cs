using PlantCare.Api.Dtos;

namespace PlantCare.Api.Services;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Partitions plants into overdue / due today / upcoming buckets by reusing
/// <see cref="IPlantService"/> (and through it <see cref="IWateringScheduleService"/>)
/// as the single source of truth for due-status computation.
/// </summary>
public sealed class DashboardService(IPlantService plants) : IDashboardService
{
    public async Task<DashboardResponseDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var all = await plants.ListAsync(cancellationToken);

        var overdue = all
            .Where(p => p.DueStatus == PlantDueStatus.Overdue)
            .OrderBy(p => p.DaysUntilDue)
            .ThenBy(p => p.NickName, StringComparer.Ordinal)
            .ToList();

        var dueToday = all
            .Where(p => p.DueStatus == PlantDueStatus.DueToday)
            .OrderBy(p => p.NickName, StringComparer.Ordinal)
            .ToList();

        // Catch-all bucket: everything not due yet, plus unscheduled plants (sorted last).
        var upcoming = all
            .Where(p => p.DueStatus is not (PlantDueStatus.Overdue or PlantDueStatus.DueToday))
            .OrderBy(p => p.DaysUntilDue ?? int.MaxValue)
            .ThenBy(p => p.NickName, StringComparer.Ordinal)
            .ToList();

        return new DashboardResponseDto
        {
            Overdue = overdue,
            DueToday = dueToday,
            Upcoming = upcoming,
        };
    }
}
