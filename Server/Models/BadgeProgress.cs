namespace FarmerQuest.Server.Models;

/// <summary>Badge/slot progress: Points 0–300 (Bronze 100 / Silver 200 / Gold 300).</summary>
public class BadgeProgress
{
    public const int MaxPoints = 300;

    public long BadgeProgressId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public PlayerStat PlayerStat { get; set; } = null!;
    // Stable key identifying which badge/slice this row tracks
    public string BadgeKey { get; set; } = string.Empty;
    // 0–300 points for this badge/slice
    public int Points { get; set; }
    public int Completions { get; set; }
    public int BestCompletions { get; set; }
    // Gray | Bronze | Silver | Gold
    public string Tier { get; set; } = "Gray";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Clamps points and derives Completions, BestCompletions, and Tier
    public void ApplyPoints(int points)
    {
        Points = Math.Clamp(points, 0, MaxPoints);
        Completions = Points >= 100 ? 1 : 0;
        BestCompletions = Points >= 300 ? 3 : Points >= 200 ? 2 : Points >= 100 ? 1 : 0;
        Tier = TierFromPoints(Points);
        UpdatedAt = DateTime.UtcNow;
    }

    // Maps point thresholds to tier names
    public static string TierFromPoints(int points) => points switch
    {
        >= 300 => "Gold",
        >= 200 => "Silver",
        >= 100 => "Bronze",
        _ => "Gray",
    };
}
