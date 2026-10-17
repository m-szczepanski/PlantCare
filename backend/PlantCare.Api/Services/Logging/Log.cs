namespace PlantCare.Api.Services.Logging;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Database not ready (attempt {Attempt}); retrying migration.")]
    public static partial void DatabaseNotReady(ILogger logger, Exception exception, int attempt);
}
