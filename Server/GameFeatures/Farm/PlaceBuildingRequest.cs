namespace FarmerQuest.Server.GameFeatures.Farm;

public class PlaceBuildingRequest
{
    public int X { get; set; }
    public int Y { get; set; }

    public string BuildingType { get; set; } = string.Empty;
}