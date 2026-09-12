namespace FarmerQuest.Server.Models;

/// <summary>
/// A multiplayer/lobby session. The host creates a session (short code
/// + QR link), and others join via code or invite. Mode matches the Unity client's
/// GameMode: 0 = Singleplayer, 2 = Multiplayer (several players together).
/// </summary>
public class GameSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    // Join code (e.g. "K7P2QX")
    public string Code { get; set; } = string.Empty;

    // >Unity GameMode: 0 = singleplayer, 2 = multiplayer
    public int Mode { get; set; }

    // Which game: "farm" or "placement"
    public string? GameKind { get; set; }

    // Link to FarmSave when GameKind is farm
    public string? FarmSaveId { get; set; }

    // Current temporary session host (not necessarily FarmSave owner)
    public string HostUserId { get; set; } = string.Empty;

    // Unused - kept in the database for backward compatibility
    public string? ActiveDriverUserId { get; set; }

    // "lobby" | "playing" | "ended"
    public string Status { get; set; } = "lobby";

    // Latest game state from a player. SignalR pushes to the group; pollers fetch via REST
    public string? StateJson { get; set; }

    // Incremented on each state update so pollers can detect new data
    public int StateVersion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GameSessionPlayer> Players { get; set; } = new List<GameSessionPlayer>();
}
