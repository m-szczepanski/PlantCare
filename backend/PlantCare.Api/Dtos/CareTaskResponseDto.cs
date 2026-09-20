using System.ComponentModel.DataAnnotations;
using PlantCare.Api.Models;
namespace PlantCare.Api.Dtos;

public class CareTaskResponseDto
{
    public int Id { get; set; }

    public required CareTaskType Type { get; set; }

    public int? IntervalDays { get; set; }

    public DateTime? LastDoneAt { get; set; }

    public bool? ReduceInWinter { get; set; }

    public PlantDueStatus DueStatus { get; set; }

    public int? DaysUntilDue { get; set; }

    public DateTime? NextDueDate { get; set; }

    public required string DueMessage { get; set; }

    public required bool InWinterNow { get; set; }

    /// <summary>Seasonal or flush reminder for this task, if any.</summary>
    public string? Hint { get; set; }
}

public class CreateCareTaskRequestDto
{
    [Required]
    public PlantCare.Api.Models.CareTaskType Type { get; set; }

    [Range(1, 3650)]
    public int IntervalDays { get; set; }

    public bool ReduceInWinter { get; set; }
}
