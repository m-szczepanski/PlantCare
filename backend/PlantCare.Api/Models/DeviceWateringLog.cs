namespace PlantCare.Api.Models;

/// <summary>
/// A water command the app sent to a device via the auto-water job. ConfirmedAt
/// is stamped when the device reports "watered"; unconfirmed rows past the
/// feedback timeout are reported once (ReportedAt).
/// </summary>
public class DeviceWateringLog
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public int DeviceId { get; set; }

    public DateTime SentAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? ReportedAt { get; set; }
}
