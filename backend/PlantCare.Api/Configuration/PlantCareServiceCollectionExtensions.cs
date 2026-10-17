using System.Text.Json.Serialization;
using Coravel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlantCare.Api.Data;
using PlantCare.Api.Services;
using PlantCare.Api.Services.Localization;

namespace PlantCare.Api.Configuration;

/// <summary>Registers every PlantCare service so the composition root stays thin.</summary>
public static class PlantCareServiceCollectionExtensions
{
    public static WebApplicationBuilder AddPlantCare(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;

        var connectionString = PlantCareConfiguration.ConnectionString(configuration);
        var usePostgres = PlantCareConfiguration.UsesPostgres(configuration);
        var devicesEnabled = configuration.GetValue("ENABLE_DEVICES", false);

        services.AddDbContext<AppDbContext>(options =>
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
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
        });

        services.AddHttpContextAccessor();
        services.AddOpenApi();
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // [ApiController]'s automatic 400s localize like every other ProblemDetails; the
        // per-field errors dictionary passes through unchanged so the SPA can map it onto
        // form fields.
        services.Configure<ApiBehaviorOptions>(options =>
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

        services.AddSingleton(new FeatureFlags(
            configuration.GetValue("ENABLE_CARE_TIPS", true),
            devicesEnabled));

        services.AddSingleton(new MqttOptions(
            configuration["MQTT_HOST"] ?? "mqtt",
            PlantCareConfiguration.Port(configuration, "MQTT_PORT", 1883),
            configuration["MQTT_USERNAME"],
            configuration["MQTT_PASSWORD"],
            configuration["MQTT_TOPIC_PREFIX"] ?? "plantcare"));

        // The bus and its consumers are registered unconditionally so controllers that
        // depend on them always resolve; the bus only *connects* (as a hosted service)
        // when the feature is enabled, so a disabled install never reaches for a broker.
        services.AddSingleton<MqttDeviceBus>();
        services.AddSingleton<IDeviceBus>(sp => sp.GetRequiredService<MqttDeviceBus>());
        services.AddSingleton(new DeviceStateOptions(
            configuration.GetValue("MOISTURE_WET_THRESHOLD_PCT", 60),
            configuration.GetValue("SOIL_WET_DEFER_DAYS", 1)));
        services.AddScoped<IDeviceStateIngestor, DeviceStateIngestor>();
        services.AddScoped<IDeviceCommandService, DeviceCommandService>();

        services.AddSingleton(new DeviceWateringOptions(
            configuration.GetValue("WATER_MAX_DURATION_MS", 60_000),
            configuration.GetValue("MOISTURE_AUTO_WATER_THRESHOLD_PCT", 40),
            configuration.GetValue("ALLOW_AUTO_WATER_WITHOUT_READING", false),
            configuration.GetValue("AUTO_WATER_MAX_PER_DAY", 1),
            configuration.GetValue("WATER_FEEDBACK_TIMEOUT_MS", 30_000)));
        services.AddScoped<IDeviceWateringService, DeviceWateringService>();

        if (devicesEnabled)
        {
            services.AddHostedService(sp => sp.GetRequiredService<MqttDeviceBus>());
            services.AddHostedService<DeviceStateService>();
        }

        var defaultLanguage = configuration["APP_LANGUAGE"] ?? Messages.DefaultLanguage;
        services.AddSingleton(new AppLanguageOptions(
            Messages.IsSupported(defaultLanguage) ? defaultLanguage : Messages.DefaultLanguage));
        services.AddScoped<IAppLocalizer, RequestAppLocalizer>();

        services.AddSingleton(new PlantPhotoOptions(configuration["PHOTO_STORAGE_PATH"] ?? "/data/uploads"));
        services.AddSingleton<IPlantPhotoStorage, PlantPhotoStorage>();

        services.AddScoped<IWateringScheduleService, WateringScheduleService>();
        services.AddScoped<IPlantService, PlantService>();
        services.AddScoped<IPlantProfileService, PlantProfileService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<ICareTaskService, CareTaskService>();
        services.AddScoped<IHealthCheckService, HealthCheckService>();
        services.AddScoped<IJournalService, JournalService>();
        services.AddScoped<IExportImportService, ExportImportService>();
        services.AddScoped<IStatusService, StatusService>();
        services.AddHttpClient();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IInsightsService, InsightsService>();

        services.AddSingleton(new NtfyOptions(
            configuration["NTFY_URL"] ?? "http://ntfy:80",
            configuration["NTFY_TOPIC"] ?? "plant-care",
            PlantCareConfiguration.OptionalPort(configuration, "NTFY_PUBLIC_PORT")));
        services.AddSingleton(new QuickActionOptions(
            configuration["QUICK_ACTION_SECRET"],
            configuration["QUICK_ACTION_URL_BASE"]));
        services.AddSingleton<QuickActionRateLimiter>();
        services.AddHttpClient<INtfyPublisher, NtfyPublisher>();
        services.AddSingleton<INotificationChannel, NtfyChannel>();
        services.AddScoped<INotificationTestService, NotificationTestService>();

        var telegramToken = configuration["TELEGRAM_BOT_TOKEN"];
        var telegramChatId = configuration["TELEGRAM_CHAT_ID"];
        if (!string.IsNullOrWhiteSpace(telegramToken) && !string.IsNullOrWhiteSpace(telegramChatId))
        {
            services.AddSingleton(new TelegramOptions(telegramToken, telegramChatId));
            services.AddHttpClient<INotificationChannel, TelegramChannel>();
        }

        services.AddScheduler();
        services.AddScoped<IWateringCheckService, WateringCheckService>();
        services.AddTransient<WateringCheckJob>();
        services.AddTransient<DeviceWateringJob>();

        return builder;
    }
}
