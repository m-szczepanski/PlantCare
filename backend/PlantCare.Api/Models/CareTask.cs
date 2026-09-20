namespace PlantCare.Api.Models;

public enum CareTaskType
{
    Watering,
}

public enum WateringMethod
{
    Tap,
    Filtered,
    Rainwater,
}

/// <summary>
/// A recurring care activity for a plant (one row per plant+type). The
/// watering task replaces the old custom-interval/last-watered columns on
/// <see cref="Plant"/>; <see cref="IntervalDays"/> null falls back to the
/// plant profile's default for watering.
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
    /// Doubles the interval during winter months (Dec-Feb, northern hemisphere)
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

    /// <summary>Watering-only detail: amount in millilitres.</summary>
    public int? AmountMilliliters { get; set; }

    /// <summary>Watering-only detail: tap / filtered / rainwater.</summary>
    public WateringMethod? Method { get; set; }
}
