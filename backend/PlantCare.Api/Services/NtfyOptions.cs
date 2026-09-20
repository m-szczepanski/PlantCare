namespace PlantCare.Api.Services;

public sealed record NtfyOptions(string BaseUrl, string Topic);

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
