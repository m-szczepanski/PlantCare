using Coravel;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Configuration;
using PlantCare.Api.Data;
using PlantCare.Api.Seed;
using PlantCare.Api.Services;
using PlantCare.Api.Services.Logging;

// The API has always exchanged naive-UTC DateTime values (SQLite stored them as
// strings). Npgsql 6+ rejects Kind=Unspecified for timestamptz parameters, so
// restore the legacy behavior of reading/writing them as UTC.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.AddPlantCare();

var app = builder.Build();
var usePostgres = PlantCareConfiguration.UsesPostgres(builder.Configuration);

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
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "PlantCare API v1");
});
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
