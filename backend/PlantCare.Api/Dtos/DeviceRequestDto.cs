using System.ComponentModel.DataAnnotations;
using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class DeviceRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public required string Name { get; set; }

    public DeviceType Type { get; set; } = DeviceType.Valve;

    public CommandStyle CommandStyle { get; set; } = CommandStyle.Pulse;

    [StringLength(200)]
    public string? CommandTopic { get; set; }

    [StringLength(200)]
    public string? StateTopic { get; set; }
}
