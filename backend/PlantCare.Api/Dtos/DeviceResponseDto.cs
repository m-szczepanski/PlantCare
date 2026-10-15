using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class DeviceResponseDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DeviceType Type { get; set; }

    public CommandStyle CommandStyle { get; set; }

    public string? CommandTopic { get; set; }

    public string? StateTopic { get; set; }

    public int? LastMoisturePct { get; set; }

    public DeviceWaterLevel? WaterLevel { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public required int PlantCount { get; set; }
}
