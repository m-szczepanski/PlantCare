using System.ComponentModel.DataAnnotations;

namespace PlantCare.Api.Dtos;

public class SnoozeRequestDto
{
    [Range(1, 365)]
    public int Days { get; set; }
}

public class SnoozeAllResponseDto
{
    public required int SnoozedPlants { get; set; }
}

/// <summary>Defers a watering because the soil is still wet (short horizon).</summary>
public class SoilWetRequestDto
{
    [Range(1, 30)]
    public int Days { get; set; }
}
