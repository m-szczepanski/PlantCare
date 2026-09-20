using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class WateringLogResponseDto
{
    public int Id { get; set; }

    public DateTime WateredAt { get; set; }

    public string? Note { get; set; }

    public int? AmountMilliliters { get; set; }

    public WateringMethod? Method { get; set; }
}
