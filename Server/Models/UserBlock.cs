namespace FarmerQuest.Server.Models;

/// <summary>
/// Block User
/// </summary>
public class UserBlock
{
    public long Id { get; set; }

    // User who initiated the block
    public string BlockerUserId { get; set; } = string.Empty;

    // User who is blocked
    public string BlockedUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
