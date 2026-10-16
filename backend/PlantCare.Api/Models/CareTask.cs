namespace PlantCare.Api.Models;

public enum CareTaskType
{
    Watering,
    Fertilizing,
    Repotting,
    TopUpWatering,
}

public enum WateringMethod
{
    Tap,
    Filtered,
    Rainwater,
}

/// <summary>
/// A recurring care activity for a plant (one row per plant+type).
/// <see cref="IntervalDays"/> null falls back to the plant profile's default for watering.
/// </summary>
public class CareTask
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    public CareTaskType Type { get; set; }

    public int? IntervalDays { get; set; }

    public DateTime? LastDoneAt { get; set; }

    /// <summary>
    /// Doubles the interval during winter months (Dec–Feb, northern hemisphere)
    /// when true; null falls back to the plant profile's default, then false.
    /// </summary>
    public bool? ReduceInWinter { get; set; }

    public List<CareTaskLog> Logs { get; set; } = [];
}

public class CareTaskLog
{
    public int Id { get; set; }

    public int CareTaskId { get; set; }

    public CareTask CareTask { get; set; } = null!;

    public DateTime DoneAt { get; set; }

    public string? Note { get; set; }

    public int? AmountMilliliters { get; set; }

    public WateringMethod? Method { get; set; }
}
