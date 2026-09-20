namespace PlantCare.Api.Dtos;

public class JournalEntryResponseDto
{
    public int Id { get; set; }

    public DateTime EntryDate { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Text { get; set; }
}
