namespace FarmerQuest.Server.GameFeatures.Farm;

public class BuildingPlacement
{
    public int Id {get; set;}

    public Guid UserId {get; set;}


//position 0-2
    public int X {get; set;}

    public int Y {get; set;}


    public string BuildingType {get; set;} = string.Empty;
}