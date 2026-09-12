namespace FarmerQuest.Server.GameFeatures.Farm;

public class BuildingPlacementDto
{
    public int Id { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public string BuildingType { get; set; } = string.Empty;
}