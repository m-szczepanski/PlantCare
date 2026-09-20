using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class CareTaskResponseDto
{
    public int Id { get; set; }

    public required CareTaskType Type { get; set; }

    public int? IntervalDays { get; set; }

    public DateTime? LastDoneAt { get; set; }

    public PlantDueStatus DueStatus { get; set; }

    public int? DaysUntilDue { get; set; }

    public DateTime? NextDueDate { get; set; }

    public required string DueMessage { get; set; }
}
