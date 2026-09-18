using System.Text.Json.Serialization;
using Coravel;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Seed;
using PlantCare.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=plantcare.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton(new FeatureFlags(builder.Configuration.GetValue("ENABLE_CARE_TIPS", true)));

builder.Services.AddScoped<IWateringScheduleService, WateringScheduleService>();
builder.Services.AddScoped<IPlantService, PlantService>();
builder.Services.AddScoped<IPlantProfileService, PlantProfileService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var ntfyBaseUrl = builder.Configuration["NTFY_URL"] ?? "http://ntfy:80";
var ntfyTopic = builder.Configuration["NTFY_TOPIC"] ?? "plant-care";
builder.Services.AddSingleton(new NtfyOptions(ntfyBaseUrl, ntfyTopic));
builder.Services.AddHttpClient<INtfyPublisher, NtfyPublisher>();

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
