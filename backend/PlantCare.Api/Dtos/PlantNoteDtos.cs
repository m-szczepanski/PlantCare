using System.ComponentModel.DataAnnotations;

namespace PlantCare.Api.Dtos;

public class PlantNoteRequestDto
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public required string Text { get; set; }
}

public class PlantNoteResponseDto
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public required string Text { get; set; }
}
