namespace PlantCare.Api.Configuration;

/// <summary>Reads the flat environment configuration, failing fast on values that are present but malformed
/// instead of silently falling back to a default.</summary>
public static class PlantCareConfiguration
{
    public static string ConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString("Default") ?? "Data Source=plantcare.db";

    public static bool UsesPostgres(IConfiguration configuration) =>
        string.Equals(configuration["DB_PROVIDER"], "postgres", StringComparison.OrdinalIgnoreCase)
        || ConnectionString(configuration).Contains("Host=", StringComparison.OrdinalIgnoreCase);

    /// <summary>Reads a required TCP port, returning <paramref name="fallback"/> when unset.</summary>
    public static int Port(IConfiguration configuration, string key, int fallback)
    {
        var raw = configuration[key];
        return string.IsNullOrWhiteSpace(raw) ? fallback : ParsePort(key, raw);
    }

    /// <summary>Reads an optional TCP port, returning <c>null</c> when unset.</summary>
    public static int? OptionalPort(IConfiguration configuration, string key)
    {
        var raw = configuration[key];
        return string.IsNullOrWhiteSpace(raw) ? null : ParsePort(key, raw);
    }

    private static int ParsePort(string key, string raw) =>
        int.TryParse(raw, out var port) && port is > 0 and < 65536
            ? port
            : throw new InvalidOperationException(
                $"{key} must be an integer between 1 and 65535 (got '{raw}').");
}
