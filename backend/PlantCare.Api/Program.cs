using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Seed;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=plantcare.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var seedFile = Path.Combine(app.Environment.ContentRootPath, "Seed", "plant-profiles.json");
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedLoader).FullName!);
    await SeedLoader.LoadPlantProfilesAsync(db, seedFile, logger);
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;
