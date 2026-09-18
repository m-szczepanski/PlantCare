namespace PlantCare.Api.Dtos;

public class DashboardResponseDto
{
    public required IReadOnlyList<PlantResponseDto> Overdue { get; set; }

    public required IReadOnlyList<PlantResponseDto> DueToday { get; set; }

    public required IReadOnlyList<PlantResponseDto> Upcoming { get; set; }
}
