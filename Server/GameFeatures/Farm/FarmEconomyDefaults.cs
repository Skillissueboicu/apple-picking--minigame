namespace FarmerQuest.Server.GameFeatures.Farm;

/// <summary>
/// Shared starting values for farm money and CO2.
/// All players in the same session share one FarmGameState (same balance / same emissions).
/// </summary>
public static class FarmEconomyDefaults
{
    // Starting cash for a new farm
    public const int StartingMoney = 500;

    // Starting CO₂ level for a new farm
    public const float StartingCo2 = 0f;

    // Starting climate score (0–100 scale) for a new farm
    public const float StartingClimateScore = 50f;
}
