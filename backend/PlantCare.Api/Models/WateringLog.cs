namespace PlantCare.Api.Models;

public class WateringLog
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    public DateTime WateredAt { get; set; }

    public string? Note { get; set; }
}
