namespace PlantCare.Api.Dtos;

/// <summary>
/// Care tips embedded in the plant detail response only when the profile is
/// linked and the ENABLE_CARE_TIPS flag is on; otherwise the UI renders nothing.
/// </summary>
public class PlantCareTipsDto
{
    public required string CommonName { get; set; }

    public required string LightRequirement { get; set; }

    public required string HumidityNotes { get; set; }

    public required string CareTips { get; set; }
}
