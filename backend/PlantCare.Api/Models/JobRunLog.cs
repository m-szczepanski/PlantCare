namespace PlantCare.Api.Models;

/// <summary>Audit row written on every watering-check run, for the status page.</summary>
public class JobRunLog
{
    public int Id { get; set; }

    public DateTime RanAt { get; set; }

    public int SentDigests { get; set; }

    public int SkippedDuplicates { get; set; }

    public int Failed { get; set; }

    public required string Outcome { get; set; }
}
