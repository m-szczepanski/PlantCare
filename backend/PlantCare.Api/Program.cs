using System.Text.Json.Serialization;
using Coravel;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Seed;
using PlantCare.Api.Services;

// The API has always exchanged naive-UTC DateTime values (SQLite stored them as
// strings). Npgsql 6+ rejects Kind=Unspecified for timestamptz parameters, so
// restore the legacy behavior of reading/writing them as UTC.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=plantcare.db";
var usePostgres = string.Equals(builder.Configuration["DB_PROVIDER"], "postgres", StringComparison.OrdinalIgnoreCase)
    || connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase);
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (usePostgres)
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }

    // The migration history is provider-portable (no hard-coded column types); the
    // snapshot was produced by the SQLite provider, so the pending-model check fires
    // against Npgsql's type defaults for the same schema. SQLite keeps the guard.
    if (usePostgres)
    {
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddOpenApi();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton(new FeatureFlags(builder.Configuration.GetValue("ENABLE_CARE_TIPS", true)));

var defaultLanguage = builder.Configuration["APP_LANGUAGE"] ?? "en";
builder.Services.AddSingleton(new AppLanguageOptions(
    PlantCare.Api.Services.Localization.Messages.IsSupported(defaultLanguage) ? defaultLanguage : "en"));
builder.Services.AddScoped<IAppLocalizer, RequestAppLocalizer>();

var photoStoragePath = builder.Configuration["PHOTO_STORAGE_PATH"] ?? "/data/uploads";
builder.Services.AddSingleton(new PlantPhotoOptions(photoStoragePath));
builder.Services.AddSingleton<IPlantPhotoStorage, PlantPhotoStorage>();

builder.Services.AddScoped<IWateringScheduleService, WateringScheduleService>();
builder.Services.AddScoped<IPlantService, PlantService>();
builder.Services.AddScoped<IPlantProfileService, PlantProfileService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<ICareTaskService, CareTaskService>();
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IExportImportService, ExportImportService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IInsightsService, InsightsService>();

var ntfyBaseUrl = builder.Configuration["NTFY_URL"] ?? "http://ntfy:80";
var ntfyTopic = builder.Configuration["NTFY_TOPIC"] ?? "plant-care";
builder.Services.AddSingleton(new NtfyOptions(ntfyBaseUrl, ntfyTopic));
var quickActionSecret = builder.Configuration["QUICK_ACTION_SECRET"];
var quickActionBaseUrl = builder.Configuration["QUICK_ACTION_URL_BASE"];
builder.Services.AddSingleton(new QuickActionOptions(quickActionSecret, quickActionBaseUrl));
builder.Services.AddSingleton<QuickActionRateLimiter>();
builder.Services.AddHttpClient<INtfyPublisher, NtfyPublisher>();
builder.Services.AddSingleton<INotificationChannel, NtfyChannel>();
var telegramToken = builder.Configuration["TELEGRAM_BOT_TOKEN"];
var telegramChatId = builder.Configuration["TELEGRAM_CHAT_ID"];
if (!string.IsNullOrWhiteSpace(telegramToken) && !string.IsNullOrWhiteSpace(telegramChatId))
{
    builder.Services.AddSingleton(new TelegramOptions(telegramToken, telegramChatId));
    builder.Services.AddHttpClient<INotificationChannel, TelegramChannel>();
}

builder.Services.AddScheduler();
builder.Services.AddScoped<IWateringCheckService, WateringCheckService>();
builder.Services.AddTransient<WateringCheckJob>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // With the optional `postgres` compose profile the database container may start
    // slightly after the API; retry instead of crash-looping.
    for (var attempt = 1; ; attempt += 1)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (usePostgres && attempt < 10)
        {
            app.Logger.LogWarning(ex, "Database not ready (attempt {Attempt}); retrying migration.", attempt);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    var seedDirectory = Path.Combine(app.Environment.ContentRootPath, "Seed");
    var seedFile = Path.Combine(seedDirectory, "plant-profiles.json");
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedLoader).FullName!);
    await SeedLoader.LoadPlantProfilesAsync(db, seedFile, logger);
    await SeedLoader.LoadCustomProfilesAsync(
        db,
        builder.Configuration["SEED_CUSTOM_PATH"] ?? "/data/seed-custom",
        logger);
    await SeedLoader.LoadProfileTranslationsAsync(db, seedDirectory, logger);
}

app.MapOpenApi();
app.MapControllers();

var schedulerLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Coravel.Scheduler");
app.Services
    .UseScheduler(scheduler => scheduler.Schedule<WateringCheckJob>().Cron(builder.Configuration["WATERING_CHECK_CRON"] ?? "0 8 * * *"))
    .OnError(ex => schedulerLogger.LogError(ex, "A scheduled task threw an unhandled exception."));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;
