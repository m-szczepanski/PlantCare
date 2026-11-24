namespace PlantCare.Api.Models;

/// <summary>
/// Who set the plant's <see cref="Plant.SoilWetUntil"/> deferral. Device state
/// ingestion only ever takes over Sensor-owned (or lapsed) deferrals, so a
/// human "soil is still wet" call is never silently erased or shortened by a
/// dry sensor reading.
/// </summary>
public enum SoilWetSource
{
    Manual,
    Sensor,
}
