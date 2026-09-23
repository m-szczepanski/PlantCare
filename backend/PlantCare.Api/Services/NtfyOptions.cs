namespace PlantCare.Api.Services;

/// <summary>
/// <see cref="BaseUrl"/> is the server-side (Docker network) origin used for
/// publishing. <see cref="PublicPort"/> optionally overrides the port of the
/// browser-facing subscribe URL, which is otherwise built from the incoming
/// request's host so it adapts to wherever the app is served from.
/// </summary>
public sealed record NtfyOptions(string BaseUrl, string Topic, int? PublicPort = null);

/// <summary>
/// Shared-secret quick actions: when <see cref="Secret"/> is set, the watering
/// digest gets an ntfy action button that POSTs back to /api/plants/{id}/quick-water?key=SECRET.
/// </summary>
public sealed record QuickActionOptions(string? Secret, string? PublicBaseUrl)
{
    public bool Enabled => !string.IsNullOrWhiteSpace(Secret);

    public string QuickWaterUrl(int plantId) =>
        $"{(PublicBaseUrl?.TrimEnd('/') ?? "")}/api/plants/{plantId}/quick-water?key={Uri.EscapeDataString(Secret!)}";
}
