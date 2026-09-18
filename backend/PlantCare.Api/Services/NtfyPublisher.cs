namespace PlantCare.Api.Services;

/// <summary>
/// Plain HTTP publisher for a self-hosted ntfy server. Throws on transport/HTTP
/// failure — callers decide per-message failure policy (the watering check
/// swallows and logs so one bad send never aborts the run).
/// </summary>
public interface INtfyPublisher
{
    Task PublishAsync(string title, string message, CancellationToken cancellationToken = default);
}

public sealed class NtfyPublisher(HttpClient http, NtfyOptions options, ILogger<NtfyPublisher> logger) : INtfyPublisher
{
    public async Task PublishAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl.TrimEnd('/')}/{options.Topic}");
        request.Headers.TryAddWithoutValidation("Title", title);
        request.Content = new StringContent(message);

        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Published ntfy message to topic {Topic}.", options.Topic);
    }
}
