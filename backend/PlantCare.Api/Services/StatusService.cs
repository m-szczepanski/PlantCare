using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;

namespace PlantCare.Api.Services;

public sealed record StatusInfo(
    DateTime NowUtc,
    string TimeZoneId,
    string WateringCheckCron,
    JobRunStatus? LastJobRun,
    DigestStatus? LastDigest,
    NtfyStatus Ntfy);

public sealed record JobRunStatus(DateTime RanAt, string Outcome, int SentDigests, int Failed);

public sealed record DigestStatus(DateTime SentAt, int PlantCount, int OverdueCount, int Priority);

public sealed record NtfyStatus(string BaseUrl, string Topic, string SubscribeUrl, bool Reachable, int? LatencyMs, string? Error);

public interface IStatusService
{
    Task<StatusInfo> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class StatusService(
    AppDbContext db,
    IHttpClientFactory httpFactory,
    NtfyOptions ntfy,
    IConfiguration configuration) : IStatusService
{
    public async Task<StatusInfo> GetAsync(CancellationToken cancellationToken = default)
    {
        var lastRun = await db.JobRuns
            .AsNoTracking()
            .OrderByDescending(j => j.RanAt)
            .FirstOrDefaultAsync(cancellationToken);
        var lastDigest = await db.NotificationDigests
            .AsNoTracking()
            .OrderByDescending(d => d.SentAt)
            .FirstOrDefaultAsync(cancellationToken);

        var ntfyStatus = await ProbeNtfyAsync(cancellationToken);

        return new StatusInfo(
            DateTime.UtcNow,
            TimeZoneInfo.Local.Id,
            configuration["WATERING_CHECK_CRON"] ?? "0 8 * * *",
            lastRun is null ? null : new JobRunStatus(lastRun.RanAt, lastRun.Outcome, lastRun.SentDigests, lastRun.Failed),
            lastDigest is null ? null : new DigestStatus(lastDigest.SentAt, lastDigest.PlantCount, lastDigest.OverdueCount, lastDigest.Priority),
            ntfyStatus);
    }

    private async Task<NtfyStatus> ProbeNtfyAsync(CancellationToken cancellationToken)
    {
        var baseUrl = ntfy.BaseUrl.TrimEnd('/');
        var subscribeUrl = $"{baseUrl}/{ntfy.Topic}";
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var http = httpFactory.CreateClient("ntfy-probe");
            http.Timeout = TimeSpan.FromSeconds(2);
            using var response = await http.GetAsync(baseUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            stopwatch.Stop();
            return new NtfyStatus(baseUrl, ntfy.Topic, subscribeUrl, response.IsSuccessStatusCode, (int)stopwatch.ElapsedMilliseconds, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            return new NtfyStatus(baseUrl, ntfy.Topic, subscribeUrl, false, null, ex.Message);
        }
    }
}
