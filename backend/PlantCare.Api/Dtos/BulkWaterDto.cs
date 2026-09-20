using System.ComponentModel.DataAnnotations;

namespace PlantCare.Api.Dtos;

public class BulkWaterRequestDto
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<int> Ids { get; set; } = [];
}

public class BulkWaterResponseDto
{
    public required int Requested { get; set; }

    public required int Watered { get; set; }

    public required IReadOnlyList<int> SkippedIds { get; set; }
}
