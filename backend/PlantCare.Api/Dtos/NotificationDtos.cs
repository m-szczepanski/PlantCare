namespace PlantCare.Api.Dtos;

/// <summary>
/// Optional body for the test-notification endpoint. When <see cref="Message"/>
/// is blank the server sends a localized default test message.
/// </summary>
public sealed record TestNotificationRequestDto(string? Message);
