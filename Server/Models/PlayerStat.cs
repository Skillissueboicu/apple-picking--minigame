namespace FarmerQuest.Server.Models;

/// <summary>
/// CSA pillar progress: 5 categories at max 1000 XP each (Klimahandling, Naturbevaring, Energistyring, Landbrugsproduktion, Teknologi)
/// </summary>
public class PlayerStat
{
    public const int MaxXpPerCategory = 1000;

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public int TotalXp { get; set; }
    // XP for Klimahandling
    public int ClimateActionXp { get; set; }
    // XP for Naturbevaring 
    public int NatureConservationXp { get; set; }
    // XP for Energistyring
    public int EnergiManagementXp { get; set; }
    // XP for Landbrugsproduktion
    public int AgriProductionXp { get; set; }
    // XP for Teknologi
    public int TechnologyXp { get; set; }
    public int RoundsPlayed { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BadgeProgress> BadgeProgresses { get; set; } = new List<BadgeProgress>();

    // Factory for a zeroed stats row when a user is created 
    public static PlayerStat CreateDefaultForUser(string userId) => new()
    {
        UserId = userId,
        UpdatedAt = DateTime.UtcNow,
    };
}
