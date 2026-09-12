namespace FarmerQuest.Server.Models;

/// <summary>
/// Pending invitation to a multiplayer session
/// </summary>
public class GameSessionInvitation
{
    public long InvitationId { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public string InvitedUserId { get; set; } = string.Empty;

    public string InvitedByUserId { get; set; } = string.Empty;

    // Host display name for invite
    public string InvitedByName { get; set; } = string.Empty;

    // pending | accepted | declined
    public string Status { get; set; } = "pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public GameSession? Session { get; set; }
}
