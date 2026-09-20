using PlantCare.Api.Dtos;
using PlantCare.Api.Services;
using Xunit;

namespace PlantCare.Api.Tests;

public class DashboardServiceTests
{
    private sealed class FakePlantService(IReadOnlyList<PlantResponseDto> plants) : IPlantService
    {
        public Task<IReadOnlyList<PlantResponseDto>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(plants);

        public Task<PlantResponseDto?> GetAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PlantWriteResult> CreateAsync(CreatePlantRequestDto dto, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PlantWriteResult> UpdateAsync(int id, UpdatePlantRequestDto dto, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PlantResponseDto?> WaterAsync(int id, string? note, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PlantResponseDto?> UndoWaterAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<WateringLogResponseDto>?> GetWateringHistoryAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PlantPhotoResult> UploadPhotoAsync(int id, Stream content, string? contentType, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private static PlantResponseDto Plant(int id, PlantDueStatus status, int? daysUntilDue, string? nickName = null)
        => new()
        {
            Id = id,
            NickName = nickName ?? $"Plant {id}",
            RoomName = "Desk",
            DueStatus = status,
            DaysUntilDue = daysUntilDue,
            DueMessage = "n/a",
        };

    private static async Task<DashboardResponseDto> Build(params PlantResponseDto[] plants)
        => await new DashboardService(new FakePlantService(plants)).GetAsync();

    [Fact]
    public async Task EveryPlantAppearsInExactlyOneBucket()
    {
        var plants = new[]
        {
            Plant(1, PlantDueStatus.Overdue, -2),
            Plant(2, PlantDueStatus.DueToday, 0),
            Plant(3, PlantDueStatus.Upcoming, 4),
            Plant(4, PlantDueStatus.NotScheduled, null),
        };

        var dashboard = await Build(plants);

        Assert.Equal([1], dashboard.Overdue.Select(p => p.Id));
        Assert.Equal([2], dashboard.DueToday.Select(p => p.Id));
        Assert.Equal([3, 4], dashboard.Upcoming.Select(p => p.Id));

        var total = dashboard.Overdue.Count + dashboard.DueToday.Count + dashboard.Upcoming.Count;
        Assert.Equal(plants.Length, total);
    }

    [Fact]
    public async Task Overdue_SortedMostOverdueFirst()
    {
        var dashboard = await Build(
            Plant(1, PlantDueStatus.Overdue, -2),
            Plant(2, PlantDueStatus.Overdue, -9),
            Plant(3, PlantDueStatus.Overdue, -2));

        Assert.Equal([2, 1, 3], dashboard.Overdue.Select(p => p.Id));
    }

    [Fact]
    public async Task Upcoming_SoonestFirst_UnscheduledLast()
    {
        var dashboard = await Build(
            Plant(1, PlantDueStatus.Upcoming, 5),
            Plant(2, PlantDueStatus.NotScheduled, null),
            Plant(3, PlantDueStatus.Upcoming, 1));

        Assert.Equal([3, 1, 2], dashboard.Upcoming.Select(p => p.Id));
    }

    [Fact]
    public async Task NoPlants_AllBucketsEmpty()
    {
        var dashboard = await Build();

        Assert.Empty(dashboard.Overdue);
        Assert.Empty(dashboard.DueToday);
        Assert.Empty(dashboard.Upcoming);
    }
}
