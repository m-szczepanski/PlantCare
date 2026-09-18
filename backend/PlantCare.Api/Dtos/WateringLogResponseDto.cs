namespace PlantCare.Api.Dtos;

public class WateringLogResponseDto
{
    public int Id { get; set; }

    public DateTime WateredAt { get; set; }

    public string? Note { get; set; }
}
