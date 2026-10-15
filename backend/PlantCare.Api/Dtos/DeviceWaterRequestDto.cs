namespace PlantCare.Api.Dtos;

public class DeviceWaterRequestDto
{
    /// <summary>Pump run time in milliseconds; omitted → the service default.</summary>
    public int? DurationMs { get; set; }
}
