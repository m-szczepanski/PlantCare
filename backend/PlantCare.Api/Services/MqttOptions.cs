namespace PlantCare.Api.Services;

public sealed record MqttOptions(string Host, int Port, string? Username, string? Password, string TopicPrefix);
