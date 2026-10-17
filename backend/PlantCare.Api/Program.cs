using System.Text.Json.Serialization;
using Coravel;
using Microsoft.AspNetCore.Mvc;
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
        options.ConfigureWarnings(
            w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddOpenApi();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// [ApiController]'s automatic 400s localize like every other ProblemDetails; the
// per-field errors dictionary passes through unchanged so the SPA can map it onto
// form fields.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var localizer = context.HttpContext.RequestServices.GetRequiredService<IAppLocalizer>();
        return new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
        {
            Title = localizer.T("error.validation.title"),
            Detail = localizer.T("error.validation.detail"),
        });
    };
});

builder.Services.AddSingleton(new FeatureFlags(
    builder.Configuration.GetValue("ENABLE_CARE_TIPS", true),
    builder.Configuration.GetValue("ENABLE_DEVICES", false)));

var mqttHost = builder.Configuration["MQTT_HOST"] ?? "mqtt";
var mqttPort = int.TryParse(builder.Configuration["MQTT_PORT"], out var parsedMqttPort)
    && parsedMqttPort is > 0 and < 65536
    ? parsedMqttPort
    : 1883;
builder.Services.AddSingleton(new MqttOptions(
    mqttHost,
    mqttPort,
    builder.Configuration["MQTT_USERNAME"],
    builder.Configuration["MQTT_PASSWORD"],
    builder.Configuration["MQTT_TOPIC_PREFIX"] ?? "plantcare"));

// The bus and its consumers are registered unconditionally so controllers that
// depend on them always resolve; the bus only *connects* (as a hosted service)
// when the feature is enabled, so a disabled install never reaches for a broker.
builder.Services.AddSingleton<MqttDeviceBus>();
builder.Services.AddSingleton<IDeviceBus>(sp => sp.GetRequiredService<MqttDeviceBus>());
builder.Services.AddSingleton(new DeviceStateOptions(
    builder.Configuration.GetValue("MOISTURE_WET_THRESHOLD_PCT", 60),
    builder.Configuration.GetValue("SOIL_WET_DEFER_DAYS", 1)));
builder.Services.AddScoped<IDeviceStateIngestor, DeviceStateIngestor>();
builder.Services.AddScoped<IDeviceCommandService, DeviceCommandService>();

builder.Services.AddSingleton(new DeviceWateringOptions(
    builder.Configuration.GetValue("WATER_MAX_DURATION_MS", 60_000),
    builder.Configuration.GetValue("MOISTURE_AUTO_WATER_THRESHOLD_PCT", 40),
    builder.Configuration.GetValue("ALLOW_AUTO_WATER_WITHOUT_READING", false),
    builder.Configuration.GetValue("AUTO_WATER_MAX_PER_DAY", 1),
    builder.Configuration.GetValue("WATER_FEEDBACK_TIMEOUT_MS", 30_000)));
builder.Services.AddScoped<IDeviceWateringService, DeviceWateringService>();

if (builder.Configuration.GetValue("ENABLE_DEVICES", false))
{
    builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttDeviceBus>());
    builder.Services.AddHostedService<DeviceStateService>();
}

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
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ICareTaskService, CareTaskService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IExportImportService, ExportImportService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IInsightsService, InsightsService>();

var ntfyBaseUrl = builder.Configuration["NTFY_URL"] ?? "http://ntfy:80";
var ntfyTopic = builder.Configuration["NTFY_TOPIC"] ?? "plant-care";
var ntfyPublicPort = int.TryParse(builder.Configuration["NTFY_PUBLIC_PORT"], out var parsedNtfyPort)
    && parsedNtfyPort is > 0 and < 65536
    ? parsedNtfyPort
    : (int?)null;
builder.Services.AddSingleton(new NtfyOptions(ntfyBaseUrl, ntfyTopic, ntfyPublicPort));
var quickActionSecret = builder.Configuration["QUICK_ACTION_SECRET"];
var quickActionBaseUrl = builder.Configuration["QUICK_ACTION_URL_BASE"];
builder.Services.AddSingleton(new QuickActionOptions(quickActionSecret, quickActionBaseUrl));
builder.Services.AddSingleton<QuickActionRateLimiter>();
builder.Services.AddHttpClient<INtfyPublisher, NtfyPublisher>();
builder.Services.AddSingleton<INotificationChannel, NtfyChannel>();
builder.Services.AddScoped<INotificationTestService, NotificationTestService>();
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
builder.Services.AddTransient<DeviceWateringJob>();

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
            Log.DatabaseNotReady(app.Logger, ex, attempt);
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
    .UseScheduler(scheduler =>
    {
        scheduler.Schedule<WateringCheckJob>().Cron(builder.Configuration["WATERING_CHECK_CRON"] ?? "0 8 * * *");
        if (builder.Configuration.GetValue("ENABLE_DEVICES", false))
        {
            scheduler.Schedule<DeviceWateringJob>().Cron(builder.Configuration["DEVICE_WATERING_CRON"] ?? "0 8 * * *");
        }
    })
    .OnError(ex => schedulerLogger.LogError(ex, "A scheduled task threw an unhandled exception."));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Database not ready (attempt {Attempt}); retrying migration.")]
    public static partial void DatabaseNotReady(ILogger logger, Exception exception, int attempt);
}
