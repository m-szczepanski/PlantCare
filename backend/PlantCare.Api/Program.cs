using System.Text.Json.Serialization;
using Coravel;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Seed;
using PlantCare.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=plantcare.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton(new FeatureFlags(builder.Configuration.GetValue("ENABLE_CARE_TIPS", true)));

var photoStoragePath = builder.Configuration["PHOTO_STORAGE_PATH"] ?? "/data/uploads";
builder.Services.AddSingleton(new PlantPhotoOptions(photoStoragePath));
builder.Services.AddSingleton<IPlantPhotoStorage, PlantPhotoStorage>();

builder.Services.AddScoped<IWateringScheduleService, WateringScheduleService>();
builder.Services.AddScoped<IPlantService, PlantService>();
builder.Services.AddScoped<IPlantProfileService, PlantProfileService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<ICareTaskService, CareTaskService>();
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
    await db.Database.MigrateAsync();

    var seedFile = Path.Combine(app.Environment.ContentRootPath, "Seed", "plant-profiles.json");
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedLoader).FullName!);
    await SeedLoader.LoadPlantProfilesAsync(db, seedFile, logger);
}

app.MapControllers();

var schedulerLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Coravel.Scheduler");
app.Services
    .UseScheduler(scheduler => scheduler.Schedule<WateringCheckJob>().Cron(builder.Configuration["WATERING_CHECK_CRON"] ?? "0 8 * * *"))
    .OnError(ex => schedulerLogger.LogError(ex, "A scheduled task threw an unhandled exception."));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;
