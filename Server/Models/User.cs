namespace FarmerQuest.Server.Models;

/// <summary>
/// Application user account and high-level game profile fields
/// </summary>
public class User
{
    public string Id { get; set; } = string.Empty;

    // Display / login name
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Role for API access ("User", "Admin", "SuperAdmin")
    public string Role { get; set; } = "User";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string Team { get; set; } = string.Empty;

    public int TotalScore { get; set; }

    public DateTime GameStartedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    public PlayerStat? PlayerStat { get; set; }
}
