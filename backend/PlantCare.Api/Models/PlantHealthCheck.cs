namespace PlantCare.Api.Models;

/// <summary>Monthly checkup answer, ordered worst → best; drives the schedule factors in <c>HealthPolicy</c>.</summary>
public enum HealthStatus
{
    Sick,
    Bad,
    Good,
    Excellent,
}

/// <summary>
/// One monthly health checkup answer for a plant (immutable history — the
/// newest row is the current status, mirrored on <see cref="Plant.HealthStatus"/>).
/// </summary>
public class PlantHealthCheck
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    public DateTime CheckedAt { get; set; }

    public HealthStatus Status { get; set; }

    public string? Note { get; set; }
}
