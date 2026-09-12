namespace FarmerQuest.Server.Models;

/// <summary>
/// A saved farm game owned by one player. Can be played alone or with friends via session+invite
/// </summary>
public class FarmSave
{
    public string SaveId { get; set; } = Guid.NewGuid().ToString();

    // User who owns this save (persists across temporary session hosts)
    public string OwnerUserId { get; set; } = string.Empty;

    // Display name in the Load menu
    public string DisplayName { get; set; } = "Min gård";

    // Serialized farm session snapshot JSON
    public string? StateJson { get; set; }

    public int StateVersion { get; set; }

    // Quick fields for the save list without parsing JSON
    public int Money { get; set; }

    public float Co2 { get; set; }

    public int Debt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;
}
