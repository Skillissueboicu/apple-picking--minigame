namespace FarmerQuest.Server.Models;

/// <summary>
/// Friend request between two users
/// </summary>
public class FriendRequest
{
    public long Id { get; set; }

    public string FromUserId { get; set; } = string.Empty;

    public string ToUserId { get; set; } = string.Empty;

    // pending | accepted | declined
    public string Status { get; set; } = "pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
