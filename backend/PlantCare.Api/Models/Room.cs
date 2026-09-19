namespace PlantCare.Api.Models;

public enum RoomOrientation
{
    North,
    East,
    South,
    West,
}

public class Room
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public RoomOrientation? Orientation { get; set; }

    public List<Plant> Plants { get; set; } = [];
}
