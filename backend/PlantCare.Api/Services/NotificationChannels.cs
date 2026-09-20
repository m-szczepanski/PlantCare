using System.Net.Http.Json;

namespace PlantCare.Api.Services;

public sealed record NotificationMessage(
    string Title,
    string Body,
    int Priority,
    string? ClickUrl = null,
    string? ButtonUrl = null,
    string? ButtonLabel = null);

public interface INotificationChannel
{
    string Name { get; }

    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

public sealed class NtfyChannel(INtfyPublisher publisher) : INotificationChannel
{
    public string Name => "ntfy";

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        => publisher.PublishAsync(
            message.Title,
            message.Body,
            message.Priority,
            message.ClickUrl,
            message.ButtonLabel,
            message.ButtonUrl,
            cancellationToken);
}

public sealed record TelegramOptions(string BotToken, string ChatId);

/// <summary>
/// Optional Telegram channel, enabled only when TELEGRAM_BOT_TOKEN and
/// TELEGRAM_CHAT_ID are configured. Sends are plain HTTP POSTs to the Bot API;
/// failures throw like the ntfy publisher and the check handles them per channel.
/// </summary>
public sealed class TelegramChannel(
    HttpClient http,
    TelegramOptions options,
    ILogger<TelegramChannel> logger) : INotificationChannel
{
    public string Name => "telegram";

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var text = $"*{message.Title}*\n{message.Body}";
        if (message.ButtonUrl is not null)
        {
            text += $"\n[{message.ButtonLabel ?? "Water now"}]({message.ButtonUrl})";
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.telegram.org/bot{options.BotToken}/sendMessage");
        request.Content = JsonContent.Create(new
        {
            chat_id = options.ChatId,
            text,
            parse_mode = "Markdown",
            disable_web_page_preview = true,
        });

        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Published Telegram message to chat {ChatId}.", options.ChatId);
    }
}
