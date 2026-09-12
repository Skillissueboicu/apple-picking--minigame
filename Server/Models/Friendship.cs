namespace FarmerQuest.Server.Models;

/// <summary>
/// Friendship between two users. UserIdA and UserIdB are stored sorted (A &lt; B)
/// so each pair exists only once.
/// </summary>
public class Friendship
{
    public long Id { get; set; }

    // Th smaller user id of the pair.</summary>
    public string UserIdA { get; set; } = string.Empty;

    // The larger user id of the pair
    public string UserIdB { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
