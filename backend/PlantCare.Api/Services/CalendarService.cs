using System.Text;
using PlantCare.Api.Dtos;

namespace PlantCare.Api.Services;

public interface ICalendarService
{
    Task<string> BuildFeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Renders the plant due dates as an iCalendar feed of recurring all-day events,
/// reusing <see cref="IPlantService"/> for the schedule computation.
/// </summary>
public sealed class CalendarService(IPlantService plants, IAppLocalizer localizer) : ICalendarService
{
    public async Task<string> BuildFeedAsync(CancellationToken cancellationToken = default)
    {
        var scheduled = (await plants.ListAsync(cancellationToken))
            .Where(p => p.NextDueDate is not null && p.WateringIntervalDays is > 0)
            .OrderBy(p => p.Id)
            .ToList();

        var now = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
        var sb = new StringBuilder();
        AppendLine(sb, "BEGIN:VCALENDAR");
        AppendLine(sb, "VERSION:2.0");
        AppendLine(sb, "PRODID:-//PlantCare//PlantCare API//EN");
        AppendLine(sb, "CALSCALE:GREGORIAN");
        AppendLine(sb, "METHOD:PUBLISH");
        AppendLine(sb, $"X-WR-CALNAME:{localizer.T("calendar.name")}");

        foreach (var plant in scheduled)
        {
            var due = plant.NextDueDate!.Value;
            var interval = plant.WateringIntervalDays!.Value;

            AppendLine(sb, "BEGIN:VEVENT");
            AppendLine(sb, $"UID:plant-{plant.Id}@plantcare");
            AppendLine(sb, $"DTSTAMP:{now}");
            AppendLine(sb, $"DTSTART;VALUE=DATE:{due:yyyyMMdd}");
            AppendLine(sb, $"DTEND;VALUE=DATE:{due.AddDays(1):yyyyMMdd}");
            AppendLine(sb, $"RRULE:FREQ=DAILY;INTERVAL={interval}");
            AppendLine(sb, $"SUMMARY:{EscapeText(localizer.Tf("calendar.summary", plant.NickName))}");
            AppendLine(sb, $"DESCRIPTION:{EscapeText(plant.DueMessage)}");
            AppendLine(sb, "END:VEVENT");
        }

        AppendLine(sb, "END:VCALENDAR");
        return sb.ToString();
    }

    private static string EscapeText(string text)
        => text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,");

    // RFC 5545 requires CRLF line endings and line folding for long lines.
    private static void AppendLine(StringBuilder sb, string line)
    {
        while (line.Length > 74)
        {
            sb.Append(line, 0, 74).Append("\r\n ");
            line = line[74..];
        }
        sb.Append(line).Append("\r\n");
    }
}
