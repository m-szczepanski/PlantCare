namespace PlantCare.Api.Services;

public sealed record NotificationChannelResult(string Name, bool Delivered, string? Error);

public sealed record NotificationTestResult(IReadOnlyList<NotificationChannelResult> Channels, bool AnyDelivered)
{
    public int Total => Channels.Count;

    public int Delivered => Channels.Count(c => c.Delivered);
}

/// <summary>
/// Sends an ad-hoc test message through every configured <see cref="INotificationChannel"/>
/// so a user can verify end-to-end delivery (phone subscription, Telegram chat) without
/// waiting for the scheduled digest or forcing a plant overdue. Every channel is attempted
/// and its outcome reported; a single failing channel never aborts the run or throws.
/// </summary>
public interface INotificationTestService
{
    Task<NotificationTestResult> SendTestAsync(string? customMessage, CancellationToken cancellationToken = default);
}

public sealed class NotificationTestService(
    IEnumerable<INotificationChannel> channels,
    IAppLocalizer localizer,
    ILogger<NotificationTestService> logger) : INotificationTestService
{
    public async Task<NotificationTestResult> SendTestAsync(string? customMessage, CancellationToken cancellationToken = default)
    {
        var title = localizer.T("test.title");
        var body = string.IsNullOrWhiteSpace(customMessage) ? localizer.T("test.body") : customMessage.Trim();
        var message = new NotificationMessage(title, body, 3);

        var results = new List<NotificationChannelResult>();
        foreach (var channel in channels)
        {
            try
            {
                await channel.SendAsync(message, cancellationToken);
                logger.LogInformation("Test notification delivered via channel {Channel}.", channel.Name);
                results.Add(new NotificationChannelResult(channel.Name, true, null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Test notification failed on channel {Channel}.", channel.Name);
                results.Add(new NotificationChannelResult(channel.Name, false, ex.Message));
            }
        }

        return new NotificationTestResult(results, results.Any(r => r.Delivered));
    }
}
