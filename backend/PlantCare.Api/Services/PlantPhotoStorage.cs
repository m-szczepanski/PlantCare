namespace PlantCare.Api.Services;

public sealed record PlantPhotoOptions(string RootPath);

/// <summary>
/// Stores managed plant photos under {RootPath}/plants/{plantId}/{guid}{ext} and
/// exposes them through the public URL prefix /uploads/ (served by nginx from the
/// same volume in Docker).
/// </summary>
public interface IPlantPhotoStorage
{
    Task<string> SaveAsync(int plantId, Stream content, string contentType, CancellationToken cancellationToken = default);

    void DeleteIfManaged(string? photoUrl);

    void DeletePlantDirectory(int plantId);
}

public sealed class PlantPhotoStorage(PlantPhotoOptions options) : IPlantPhotoStorage
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public const string PublicUrlPrefix = "/uploads/";

    private static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    public static bool TryGetExtension(string? contentType, out string extension)
    {
        extension = string.Empty;
        var mediaType = contentType?.Split(';')[0].Trim();
        return mediaType is not null && AllowedTypes.TryGetValue(mediaType, out extension!);
    }

    public static IReadOnlyCollection<string> SupportedContentTypes => AllowedTypes.Keys.ToList();

    public async Task<string> SaveAsync(int plantId, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        if (!TryGetExtension(contentType, out var extension))
        {
            throw new InvalidDataException($"Unsupported photo type '{contentType}'.");
        }

        if (content.CanSeek && content.Length > MaxBytes)
        {
            throw new InvalidDataException("Photo exceeds the 5 MB limit.");
        }

        var directory = Path.Combine(options.RootPath, "plants", plantId.ToString());
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(directory, fileName);
        await using (var target = File.Create(fullPath))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        return $"{PublicUrlPrefix}plants/{plantId}/{fileName}";
    }

    public void DeleteIfManaged(string? photoUrl)
    {
        if (photoUrl is null || !photoUrl.StartsWith(PublicUrlPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var relative = photoUrl[PublicUrlPrefix.Length..];
        var root = Path.GetFullPath(options.RootPath);
        var candidate = Path.GetFullPath(Path.Combine(root, relative));

        if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return;
        }

        TryDelete(() => File.Delete(candidate));
    }

    public void DeletePlantDirectory(int plantId)
    {
        var directory = Path.GetFullPath(Path.Combine(options.RootPath, "plants", plantId.ToString()));
        TryDelete(() => Directory.Delete(directory, recursive: true));
    }

    private static void TryDelete(Action delete)
    {
        try
        {
            delete();
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
