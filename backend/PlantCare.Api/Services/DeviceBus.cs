using MQTTnet;

namespace PlantCare.Api.Services;

/// <summary>MQTT topic helpers for self-watering devices — see <c>docs/hardware-integration.md</c>.</summary>
public static class DeviceTopics
{
    public static string Command(string prefix, int deviceId) => $"{prefix}/{deviceId}/command";

    public static string State(string prefix, int deviceId) => $"{prefix}/{deviceId}/state";

    public static string StatePattern(string prefix) => $"{prefix}/+/state";

    /// <summary>
    /// Extracts the device id from a state topic of the form
    /// <c>{prefix}/{deviceId}/state</c>. Returns false for anything else
    /// (custom per-device state topics are not subscribed yet). The prefix is
    /// deliberately not validated: the bus only subscribes to
    /// <see cref="StatePattern"/>, so foreign topics are never delivered.
    /// </summary>
    public static bool TryParseStateTopic(string topic, out int deviceId)
    {
        deviceId = 0;
        var parts = topic.Split('/');
        if (parts.Length < 3 || parts[^1] != "state" || !int.TryParse(parts[^2], out deviceId))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// MQTT transport for self-watering devices: publish commands, subscribe to
/// state, and surface received messages to consumers. Reconnects in the
/// background so a broker outage never crashes the API.
/// </summary>
public interface IDeviceBus
{
    /// <summary>Raised for every message received on a subscribed topic (topic, payload).</summary>
    event Func<string, string, Task>? MessageReceived;

    bool IsConnected { get; }

    Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default);
}

public sealed partial class MqttDeviceBus(MqttOptions options, ILogger<MqttDeviceBus> logger) : BackgroundService, IDeviceBus
{
    private const int ReconnectDelayMs = 5000;

    private readonly MqttClientFactory _factory = new();
    private IMqttClient? _client;

    public event Func<string, string, Task>? MessageReceived;

    public bool IsConnected => _client?.IsConnected is true;

    public async Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        var client = _client;
        if (client is null || !client.IsConnected)
        {
            throw new InvalidOperationException("MQTT client is not connected.");
        }

        var message = _factory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();

        await client.PublishAsync(message, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _client?.Dispose();
                _client = null;

                var client = _factory.CreateMqttClient();
                var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                client.DisconnectedAsync += _ =>
                {
                    logger.LogWarning("MQTT disconnected; reconnecting.");
                    disconnected.TrySetResult();
                    return Task.CompletedTask;
                };
                client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

                LogConnecting(logger, options.Host, options.Port);
                await client.ConnectAsync(BuildClientOptions(), stoppingToken);
                await client.SubscribeAsync(BuildSubscribeOptions(), stoppingToken);
                _client = client;
                LogConnected(logger, options.Host, options.Port);

                await disconnected.Task.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogConnectionFailed(logger, ex, ReconnectDelayMs);
            }

            try
            {
                await Task.Delay(ReconnectDelayMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public override void Dispose()
    {
        _client?.Dispose();
        base.Dispose();
    }

    private MqttClientOptions BuildClientOptions()
    {
        var builder = _factory.CreateClientOptionsBuilder()
            .WithTcpServer(options.Host, options.Port)
            .WithClientId("plantcare-api")
            .WithCleanSession(true)
            .WithTimeout(TimeSpan.FromSeconds(10));

        if (!string.IsNullOrEmpty(options.Username))
        {
            builder = builder.WithCredentials(options.Username, options.Password);
        }

        return builder.Build();
    }

    private MqttClientSubscribeOptions BuildSubscribeOptions()
    {
        var filter = _factory.CreateTopicFilterBuilder()
            .WithTopic(DeviceTopics.StatePattern(options.TopicPrefix))
            .Build();

        return _factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(filter)
            .Build();
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.ConvertPayloadToString();
        LogMessage(logger, topic);

        var handler = MessageReceived;
        if (handler is not null)
        {
            await handler(topic, payload);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Connecting to MQTT broker at {Host}:{Port}.")]
    private static partial void LogConnecting(ILogger logger, string host, int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected and subscribed to MQTT broker at {Host}:{Port}.")]
    private static partial void LogConnected(ILogger logger, string host, int port);

    [LoggerMessage(Level = LogLevel.Warning, Message = "MQTT connection failed; retrying in {DelayMs}ms.")]
    private static partial void LogConnectionFailed(ILogger logger, Exception exception, int delayMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "MQTT message on {Topic}.")]
    private static partial void LogMessage(ILogger logger, string topic);
}
