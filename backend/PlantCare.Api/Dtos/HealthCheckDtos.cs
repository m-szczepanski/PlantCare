using System.ComponentModel.DataAnnotations;
using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class HealthCheckResponseDto
{
    public int Id { get; set; }

    public HealthStatus Status { get; set; }

    public DateTime CheckedAt { get; set; }

    public string? Note { get; set; }
}

public class CreateHealthCheckRequestDto
{
    public HealthStatus Status { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
