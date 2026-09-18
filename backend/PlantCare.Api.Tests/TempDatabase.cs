using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace PlantCare.Api.Tests;

internal sealed class TempDatabase : IDisposable
{
    public TempDatabase()
    {
        DbPath = Path.Combine(Path.GetTempPath(), $"plantcare-tests-{Guid.NewGuid():N}.db");
        ConnectionString = $"Data Source={DbPath}";
    }

    public string DbPath { get; }

    public string ConnectionString { get; }

    public WebApplicationFactory<Program> CreateFactory(Action<IServiceCollection>? configureTestServices = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", ConnectionString);
            if (configureTestServices is not null)
            {
                builder.ConfigureTestServices(configureTestServices);
            }
        });

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            try
            {
                File.Delete(DbPath + suffix);
            }
            catch (IOException)
            {
            }
        }
    }
}
