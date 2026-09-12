namespace FarmerQuest.Server.GameFeatures.Farm;

// Building identifiers used in farm simulation and client sync
public enum FarmBuildingId
{
    Stald = 0,
    Mark = 1,
    Kompost = 2,
    Hovedhus = 3,
    Laden = 4,
}

// Resource types that can sit in building buffers or travel over connections
public enum FarmResourceType
{
    Foder = 0,
    Gylle = 1,
    Maelk = 2,
    Kompost = 3,
    Fro = 4,
    Korn = 5,
}

/// <summary>
/// Display labels for buildings
/// </summary>
public static class FarmBuildingNames
{
    // Returns the label for a building id
    public static string GetLabel(FarmBuildingId id) => id switch
    {
        FarmBuildingId.Stald => "Stald",
        FarmBuildingId.Mark => "Mark",
        FarmBuildingId.Kompost => "Kompost",
        FarmBuildingId.Hovedhus => "Hovedhus",
        FarmBuildingId.Laden => "Lade",
        _ => id.ToString(),
    };
}

/// <summary>
/// Display labels for resources
/// </summary>
public static class FarmResourceNames
{
    // Returns the label for a resource type
    public static string GetLabel(FarmResourceType type) => type switch
    {
        FarmResourceType.Foder => "Foder",
        FarmResourceType.Gylle => "Gylle",
        FarmResourceType.Maelk => "Mælk",
        FarmResourceType.Kompost => "Kompost",
        FarmResourceType.Fro => "Frø",
        FarmResourceType.Korn => "Korn",
        _ => type.ToString(),
    };
}
