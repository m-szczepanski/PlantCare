namespace PlantCare.Api.Models;

public enum RoomOrientation
{
    North,
    East,
    South,
    West,
}

public enum HumidityLevel
{
    Low,
    Medium,
    High,
}

/// <summary>
/// How far a room's actual light is from what the plant's profile asks for.
/// "Bad" values (>= 2 levels apart) drive the "wrong room" flag in the UI.
/// </summary>
public enum RoomLightMatch
{
    Good,
    SlightlyTooDark,
    SlightlyTooBright,
    MuchTooDark,
    MuchTooBright,
}

public class Room
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public RoomOrientation? Orientation { get; set; }

    public LightRequirement? LightExposure { get; set; }

    public HumidityLevel? Humidity { get; set; }

    public int? TemperatureCelsius { get; set; }

    public List<Plant> Plants { get; set; } = [];
}
