namespace PlantCare.Api.Dtos;

public class SpeciesCountDto
{
    public required int PlantProfileId { get; set; }

    public required string CommonName { get; set; }

    public required int PlantCount { get; set; }
}

public class NeglectedPlantDto
{
    public required int PlantId { get; set; }

    public required string NickName { get; set; }

    public required int DaysSinceLastWatering { get; set; }
}

public class StreakDto
{
    public required int PlantId { get; set; }

    public required string NickName { get; set; }

    public required int ConsecutiveOnTimeWaterings { get; set; }
}

public class MonthlyCountDto
{
    public required string Month { get; set; }

    public required int Count { get; set; }
}

public class InsightsResponseDto
{
    public required int TotalPlants { get; set; }

    public required int ScheduledPlants { get; set; }

    public required int UnscheduledPlants { get; set; }

    public required int SpeciesCount { get; set; }

    public required IReadOnlyList<SpeciesCountDto> Species { get; set; }

    public required IReadOnlyList<NeglectedPlantDto> MostNeglected { get; set; }

    public required int AdherenceWindowDays { get; set; }

    public required double AdherencePercent { get; set; }

    public required int ExpectedWateringsInWindow { get; set; }

    public required int ActualWateringsInWindow { get; set; }

    public required IReadOnlyList<StreakDto> Streaks { get; set; }

    public required IReadOnlyList<MonthlyCountDto> MonthlyWaterings { get; set; }
}
