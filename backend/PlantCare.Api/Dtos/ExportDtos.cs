using PlantCare.Api.Models;

namespace PlantCare.Api.Dtos;

public class ExportDocumentDto
{
    public required int SchemaVersion { get; set; }

    public required DateTime ExportedAt { get; set; }

    public required IReadOnlyList<ExportRoomDto> Rooms { get; set; }

    public required IReadOnlyList<ExportProfileDto> PlantProfiles { get; set; }

    public required IReadOnlyList<ExportPlantDto> Plants { get; set; }
}

public class ExportRoomDto
{
    public required string Name { get; set; }

    public RoomOrientation? Orientation { get; set; }

    public LightRequirement? LightExposure { get; set; }

    public HumidityLevel? Humidity { get; set; }

    public int? TemperatureCelsius { get; set; }
}

public class ExportProfileDto
{
    public required string CommonName { get; set; }

    public string? ScientificName { get; set; }

    public int DefaultWateringIntervalDays { get; set; }

    public LightRequirement LightRequirement { get; set; }

    public required string HumidityNotes { get; set; }

    public required string CareTips { get; set; }

    public bool DefaultReduceInWinter { get; set; }

    public bool ToxicToPets { get; set; }

    public bool ToxicToChildren { get; set; }

    public string? DiagnosisChecklist { get; set; }
}

public class ExportPlantDto
{
    public required string NickName { get; set; }

    public string? RoomName { get; set; }

    public string? ProfileCommonName { get; set; }

    public DateTime AcquiredDate { get; set; }

    public string? PhotoUrl { get; set; }

    public int? PotSizeCm { get; set; }

    public SoilType? SoilType { get; set; }

    public string? SoilMix { get; set; }

    public string? PropagatedFrom { get; set; }

    public bool NotifyEnabled { get; set; }

    public DateTime? SnoozedUntil { get; set; }

    public required IReadOnlyList<ExportCareTaskDto> CareTasks { get; set; }

    public required IReadOnlyList<ExportNoteDto> Notes { get; set; }

    public required IReadOnlyList<ExportJournalEntryDto> Journal { get; set; }
}

public class ExportCareTaskDto
{
    public required CareTaskType Type { get; set; }

    public int? IntervalDays { get; set; }

    public DateTime? LastDoneAt { get; set; }

    public bool? ReduceInWinter { get; set; }

    public required IReadOnlyList<ExportCareTaskLogDto> Logs { get; set; }
}

public class ExportCareTaskLogDto
{
    public DateTime DoneAt { get; set; }

    public string? Note { get; set; }

    public int? AmountMilliliters { get; set; }

    public WateringMethod? Method { get; set; }
}

public class ExportNoteDto
{
    public DateTime CreatedAt { get; set; }

    public required string Text { get; set; }
}

public class ExportJournalEntryDto
{
    public DateTime EntryDate { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Text { get; set; }
}

public class ImportResultDto
{
    public required int RoomsCreated { get; set; }

    public required int ProfilesCreated { get; set; }

    public required int PlantsCreated { get; set; }

    public required int PlantsSkipped { get; set; }
}
